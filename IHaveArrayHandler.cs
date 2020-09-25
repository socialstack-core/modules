using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Users;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Startup {

	/// <summary>
	/// Helper class for adding IHave* which defines an array of things on a type.
	/// T = the IHave* interface type
	/// U = The target content (e.g. Tag)
	/// M = The mapper type (e.g. TagContent)
	/// </summary>
	public class IHaveArrayHandler<T, U, M> 
			where T : class
			where U : DatabaseRow, new()
			where M : MappingRow, new()
	{
		/// <summary>
		/// The name of the field in the mapper type for the target content ID. E.g. "TagId".
		/// </summary>
		public string MapperFieldName;

		/// <summary>
		/// Field name in custom where handling. Optional.
		/// </summary>
		public string WhereFieldName;

		/// <summary>
		/// The action which sets results.
		/// </summary>
		public Action<T, List<U>> OnSetResult;

		/// <summary>
		/// Called when setting up the admin field.
		/// </summary>
		public Action<JsonField> OnSetupAdminField;

		/// <summary>
		/// The query helper used when listing mapping objects.
		/// </summary>
		private Query<M> listByObjectQuery;

		/// <summary>
		/// The DB service.
		/// </summary>
		public DatabaseService Database;

		/// <summary>
		/// The service which obtains the content.
		/// </summary>
		private AutoService<U> contentService;

		/// <summary>
		/// The service which obtains mappings.
		/// </summary>
		private AutoService<M> mappingService;

		/// <summary>
		/// Adds the mapping now.
		/// </summary>
		public void Map()
		{
			listByObjectQuery = Query.List<M>();
			listByObjectQuery.Where().EqualsArg("ContentTypeId", 0).And().EqualsArg("ContentId", 1).And().EqualsArg("RevisionId", 2);

			// Discover the types via looking for their AfterLoad events with the interface on them:
			var loadEvents = Events.FindByType(typeof(T), "Load", EventPlacement.After);

			var methodInfo = GetType().GetMethod("SetupHandlers");

			if (!string.IsNullOrEmpty(WhereFieldName))
			{
				// We'll handle where field with custom code:
				Filter.DeclareCustomWhereField(WhereFieldName);
			}

			foreach (var loadEvent in loadEvents)
			{
				// Get the actual type. We use this to avoid Revisions etc as we're not interested in those here:
				var contentType = ContentTypes.GetType(loadEvent.EntityName);

				if (contentType == null)
				{
					continue;
				}

				// Invoke setup for type:
				var setupType = methodInfo.MakeGenericMethod(new Type[] {
					contentType
				});

				setupType.Invoke(this, new object[] {});
			}

		}

		/// <summary>
		/// Sets a particular type with IHave* handlers. Used via reflection.
		/// </summary>
		/// <typeparam name="CT"></typeparam>
		public void SetupHandlers<CT>() where CT : DatabaseRow, T, new()
		{
			UserService _users = null;

			// Invoked by reflection
			var evtGroup = Events.GetGroup<CT>();

			// Get the content type ID for the primary object:
			var contentTypeId = ContentTypes.GetId(typeof(CT));

			evtGroup.AfterLoad.AddEventListener(async (Context context, CT content) =>
			{
				if (content == null)
				{
					// Event chaining - can be null
					return null;
				}

				int revisionId = 0;

				if (content is RevisionRow)
				{
					var revId = (content as RevisionRow).RevisionId;

					if (revId.HasValue)
					{
						revisionId = revId.Value;
					}
				}

				// List the content now:
				var mappings = await Database.List(context, listByObjectQuery, null, contentTypeId, content.Id, revisionId);

				if (contentService == null)
				{
					contentService = Services.GetByContentType(typeof(U)) as AutoService<U>;
				}

				if (mappings == null || mappings.Count == 0)
				{
					// None found - just set an empty content array.
					OnSetResult(content as T, new List<U>());
				}
				else if (mappings.Count == 1)
				{
					// Use a higher speed prepared statement:
					OnSetResult(content as T, new List<U>(1)
					{
						await contentService.Get(context, mappings[0].TargetContentId)
					});
				}
				else
				{
					// It has multiple tags. Use a filtered list here.
					var filter = new Filter<U>();
					filter.EqualsSet("Id", mappings.Select(t => t.TargetContentId));
					OnSetResult(content as T, await contentService.List(context, filter));
				}

				return content;
			});

			// Hook up a MultiSelect on the underlying fields:
			evtGroup.BeforeSettable.AddEventListener((Context context, JsonField<CT> field) =>
			{
				if (field != null && field.Name == WhereFieldName)
				{
					field.Module = "Admin/MultiSelect";
					field.Data["contentType"] = typeof(U).Name;

					if (typeof(U) == typeof(User))
					{
						field.Data["field"] = "fullName";
						field.Data["displayField"] = "fullName";
					}

					if (OnSetupAdminField != null)
					{
						OnSetupAdminField(field);
					}

					// Defer the set after the ID is available:
					field.AfterId = true;

					// On set, convert provided IDs into tag objects.
					field.OnSetValue.AddEventListener(async (Context ctx, object value, CT targetObject, JToken srcToken) =>
					{
						// The value should be an array of ints.
						if (!(value is JArray idArray))
						{
							return null;
						}

						var ids = new List<int>();

						foreach (var token in idArray)
						{
							// id is..
							var id = token.Value<int?>();

							if (id.HasValue && id > 0)
							{
								ids.Add(id.Value);
							}
						}

						int revisionId = 0;

						if (targetObject is RevisionRow)
						{
							var revId = (targetObject as RevisionRow).RevisionId;

							if (revId.HasValue)
							{
								revisionId = revId.Value;
							}
						}

						// Get all mapping entries for this object:
						var existingEntries = await Database.List(ctx, listByObjectQuery, null, contentTypeId, targetObject.Id, revisionId);

						// Identify ones being deleted, and ones being added, then update tag contents.
						Dictionary<int, T> existingLookup = new Dictionary<int, T>();

						foreach (var existingEntry in existingEntries)
						{
							existingLookup[existingEntry.TargetContentId] = null;
						}

						var now = DateTime.UtcNow;

						Dictionary<int, bool> newSet = new Dictionary<int, bool>();

						if (mappingService == null)
						{
							mappingService = Services.GetByContentType(typeof(M)) as AutoService<M>;
						}

						foreach (var id in ids)
						{
							newSet[id] = true;

							if (!existingLookup.ContainsKey(id))
							{
								// Add it:
								await mappingService.Create(ctx, new M()
								{
									ContentId = targetObject.Id,
									ContentTypeId = contentTypeId,
									TargetContentId = id,
									RevisionId = revisionId,
									CreatedUtc = now
								});
							}
						}

						// Delete any being removed:
						foreach (var existingEntry in existingEntries)
						{
							if (!newSet.ContainsKey(existingEntry.TargetContentId))
							{
								// Delete this row:
								await mappingService.Delete(ctx, existingEntry.Id);
							}
						}

						if (ids.Count == 0)
						{
							// Empty set to return.
							return null;
						}

						if (contentService == null)
						{
							contentService = Services.GetByContentType(typeof(U)) as AutoService<U>;
						}

						// Get the set:
						var contentSet = await contentService.List(ctx, new Filter<U>().EqualsSet("Id", ids));

						if (typeof(U) == typeof(User))
						{
							// Special case - return profile list.
							if (_users == null)
							{
								_users = Services.Get<UserService>();
							}

							var set = new List<Api.Users.UserProfile>();

							foreach (var user in contentSet)
							{
								set.Add(_users.GetProfile(user as User));
							}

							return set;
						}

						return contentSet;
					});


				}

				return new ValueTask<JsonField<CT>>(field);
			});

			// Next the List events:
			evtGroup.AfterList.AddEventListener(async (Context context, List<CT> content) =>
			{
				if (content == null)
				{
					return content;
				}

				// First we'll collect all their IDs so we can do a single bulk lookup.
				// ASSUMPTION: The list is not excessively long!
				// FUTURE IMPROVEMENT: Do this in chunks of ~50k entries.
				// (applies to at least categories/ tags).
				var contentLookup = new Dictionary<int, List<U>>();

				for (var i = 0; i < content.Count; i++)
				{
					// Add to content lookup so we can map the items to it shortly:
					var entry = content[i];
					var set = new List<U>();
					contentLookup[entry.Id] = set;
				}

				if (contentLookup.Count == 0)
				{
					// Nothing to do - just return here:
					return content;
				}

				// Create the filter and run the query now:
				var filter = new Filter<M>();
				filter.Equals("ContentTypeId", contentTypeId).And().EqualsSet("ContentId", contentLookup.Keys).And().Equals("RevisionId", 0);

				// Todo: The above blocks a set of things from loading on lists of revisions
				// However, such a list of revisions requires a special case of per-row querying.

				if (mappingService == null)
				{
					mappingService = Services.GetByContentType(typeof(M)) as AutoService<M>;
				}

				// Get all the mappings for these entities:
				var allMappings = await mappingService.List(context, filter);

				// Build fast lookup:
				var targetContentLookup = new Dictionary<int, U>();

				// Get the unique set of IDs so we can collect those categories. Shortly will reuse the same lookup dict.
				foreach (var contentTag in allMappings)
				{
					targetContentLookup[contentTag.TargetContentId] = null;
				}

				if (targetContentLookup.Count == 0)
				{
					// Nothing to do - just return here:
					return content;
				}

				if (contentService == null)
				{
					contentService = Services.GetByContentType(typeof(U)) as AutoService<U>;
				}

				var contentFilter = new Filter<U>();
				contentFilter.EqualsSet("Id", targetContentLookup.Keys);

				var targetContents = await contentService.List(context, contentFilter);

				foreach (var targetContent in targetContents)
				{
					targetContentLookup[targetContent.Id] = targetContent;
				}

				// For each mapping..
				foreach (var mapping in allMappings)
				{
					// Lookup target content:
					var set = contentLookup[mapping.ContentId];
					var targetContent = targetContentLookup[mapping.TargetContentId];

					// Add the result to the set:
					set.Add(targetContent);
				}

				// Trigger OnSetResult:
				// - Happens last to catch UserProfile special case (as it maps the objects when OnSetResult is called).
				for (var i = 0; i < content.Count; i++)
				{
					var entry = content[i];
					OnSetResult(entry, contentLookup[entry.Id]);
				}

				return content;
			});

			// Next we'll add a handler for filters on List endpoints.
			// These are the *List events (but unlike above, we're using the "NotSpecified" placement):
			evtGroup.List.AddEventListener(async (Context context, Filter<CT> filter, HttpResponse response) =>
			{
				// These always have:
				// args[0] is a filter object
				// args[1] is for the Response

				if (filter == null || filter.FromRequest == null)
				{
					// We can't handle this - return the first arg just in case it was something else:
					return filter;
				}

				var where = filter.FromRequest["where"] as JObject;

				if (where == null)
				{
					// No where clause
					return filter;
				}

				// If the filter contained the field we're interested in then we'll add a join restriction:
				var idSet = where[WhereFieldName] as JArray;

				if (idSet != null)
				{
					// We've got content that we need to filter by. For now this set must be integer host IDs.
					// We want to join the host content table 
					// on WhateverTypeTheFilterIsUsing.Id = HostContent.Id AND ContentTypeId = TheIDOfThatFilterType
					// AND UserId IN(hostSet)

					if (mappingService == null)
					{
						mappingService = Services.GetByContentType(typeof(M)) as AutoService<M>;
					}

					// Each piece of content must have all of these hosts to satisfy the request.
					var requiredList = await mappingService.List(context, new Filter<M>()
						.Equals("RevisionId", 0)
						.And().Equals("ContentTypeId", contentTypeId)
						.And().EqualsSet("UserId", idSet.Select(token => token.Value<int>())));

					// Build unique set of content IDs:
					Dictionary<int, bool> uniqueIds = new Dictionary<int, bool>();

					foreach (var entry in requiredList)
					{
						uniqueIds[entry.ContentId] = true;
					}

					if (uniqueIds.Count == 0)
					{
						// Force no results
						if (!filter.HasContent)
						{
							filter.EqualsField("Id", 0);
						}
						else
						{
							filter.And().EqualsField("Id", 0);
						}
					}
					else
					{
						// Restrict filter to matching those IDs:
						if (!filter.HasContent)
						{
							filter.EqualsSet("Id", uniqueIds.Keys);
						}
						else
						{
							filter.And().EqualsSet("Id", uniqueIds.Keys);
						}
					}
				}

				return filter;
			});

		}

	}

}