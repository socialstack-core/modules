using System;
using System.Reflection;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Startup;
using Api.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;


namespace Api.Revisions
{

	/// <summary>
	/// Listens out for the DatabaseDiff run to add additional revision tables, as well as BeforeUpdate events to then automatically create the revision rows.
	/// </summary>
	[EventListener]
	public class EventListener
	{

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public EventListener()
		{
			// Hook up the database diff event, which will be used to generate tables for us:
			Events.DatabaseDiffBeforeAdd.AddEventListener((Context ctx, FieldMap fieldMap, Type typeInfo, Schema newSchema) => {

				if (fieldMap == null)
				{
					return new ValueTask<FieldMap>(fieldMap);
				}

				// Firstly, is this type a RevisionRow?
				// If so, we'll need to add another table to the schema with the same set of fields only it's called _revisions.

				if (!ContentTypes.IsAssignableToGenericType(typeInfo, typeof(VersionedContent<>), out Type revisionRowType))
				{
					return new ValueTask<FieldMap>(fieldMap);
				}
				
				// Above test also eliminated any mappings.

				var revisionIdField = revisionRowType.GetField("_RevisionId", BindingFlags.Instance | BindingFlags.NonPublic);
				var isDraftField = revisionRowType.GetField("_IsDraft", BindingFlags.Instance | BindingFlags.NonPublic);
                var publishDraftDateField = revisionRowType.GetField("_PublishDraftDate", BindingFlags.Instance | BindingFlags.NonPublic);

                // We've got a revisionable content type. Add a revisions table to the schema:
                var targetEntityName = typeInfo.Name + "_revisions";
				var targetTableName = MySQLSchema.TableName(typeInfo.Name) + "_revisions";
				Field idField = null;

				// All of the fields in the type can be revisioned, so we add them all.
				// Note, however, that the Id column changes meaning. It becomes the Id of the *revision*.
				foreach (var field in fieldMap.Fields)
				{
					DatabaseColumnDefinition columnDefinition = null;
					
					// If we have the Id field..
					if (field.Name == "Id")
					{
						// Special case for the Id field, as it'll be the revision ID.
						// That means when we load it, it must go into the RevisionId field instead.
						// We can do that by cloning the field object and then simply changing the FieldInfo for the field it sets.
						idField = field;
						var specialIdField = field.Clone();

						// The field name stays the same, so it's still "Id" in the table, but the value goes to and from the RevisionId field.
						specialIdField.TargetField = revisionIdField;

						// Create a column definition:
						columnDefinition = new MySQLDatabaseColumnDefinition(specialIdField, targetTableName)
						{
							IsAutoIncrement = true
						};
					}
					else
					{
						// Create a column definition:
						columnDefinition = new MySQLDatabaseColumnDefinition(field, targetTableName);
					}

					if (!columnDefinition.Ignore)
					{
						// Add to target schema:
						newSchema.Add(columnDefinition);
					}
				}

				if (idField != null)
				{
					// Next, as the Id column means something else, we'll need to add a special column for the content's ID.
					// Similarly to above, we can do that by cloning the Id field but this time we give it a new name.
					// That'll mean its value goes to and from the Id field on objects, but it is retained in a column called RevisionOriginalContentId.
					var contentIdField = idField.Clone();
					contentIdField.Name = "RevisionOriginalContentId";
					contentIdField.SetFullName();

					var contentIdColumn = new MySQLDatabaseColumnDefinition(
						contentIdField,
						targetTableName
					)
					{

						// It may have seen that the Id column is autoinc, depending on field attributes, so clear that:
						IsAutoIncrement = false
					};

					if (!contentIdColumn.Ignore)
					{
						newSchema.Add(
							contentIdColumn
						);
					}
				}

				// Add IsDraft column:
				var isDraft = new Field(typeInfo, targetEntityName)
				{
					Type = isDraftField.FieldType,
					TargetField = isDraftField,
					Name = "RevisionIsDraft"
				};
				isDraft.SetFullName();
				newSchema.AddColumn(isDraft);

                // Add Publish Draft Date column:
                var publishDraftDate = new Field(typeInfo, targetEntityName)
                {
                    Type = publishDraftDateField.FieldType,
                    TargetField = publishDraftDateField,
                    Name = "PublishDraftDate"
                };
                publishDraftDate.SetFullName();
                newSchema.AddColumn(publishDraftDate);

                return new ValueTask<FieldMap>(fieldMap);
			});

			// Next hook up all before update events for anything which is a RevisionRow type.
			// Essentially before the content actually goes into the database, we copy the database row into the _revisions table (with 1 database-side query), 
			// and bump the Revision number of the about to be updated row.
			
			var methodInfo = GetType().GetMethod(nameof(SetupForRevisions));

			Events.Service.AfterCreate.AddEventListener((Context ctx, AutoService svc) => {
				if (svc == null || svc.ServicedType == null)
				{
					return new ValueTask<AutoService>(svc);
				}

				var eventGroup = svc.GetEventGroup();

				if (eventGroup != null && ContentTypes.IsAssignableToGenericType(svc.ServicedType, typeof(VersionedContent<>)))
				{
					// Invoke setup for type:
					var idType = svc.IdType;

					var setupType = methodInfo.MakeGenericMethod(new Type[] {
						svc.ServicedType,
						idType
					});

					setupType.Invoke(this, new object[] {
						svc
					});
				}

				return new ValueTask<AutoService>(svc);
			});
		}

		/// <summary>
		/// The database service.
		/// </summary>
		private MySQLDatabaseService database = null;

		private Query BuildCopyQuery(Type contentType, string contentTypeName)
		{
			var fieldMap = new FieldMap(contentType, contentTypeName);

			// First, generate a 'copy' query. It'll transfer values from table A to table B.
			var transferMap = new FieldTransferMap
			{
				TargetTypeNameExtension = "_revisions"
			};

			// For each field in the map, create a transfer:
			foreach (var field in fieldMap.Fields)
			{
				// Special case for the Id field.
				if (field.Name == "Id")
				{
					// Id transfers to the RevisionContentId field.
					transferMap.Add(contentType, contentTypeName, field.Name, contentType, contentTypeName, "RevisionOriginalContentId");
				}
				else
				{
					// The only difference is the target type name extension above.
					transferMap.Add(contentType, contentTypeName, field.Name, contentType, contentTypeName, field.Name);
				}
			}

			transferMap.AddConstant(contentType, contentTypeName, "RevisionIsDraft", false);

			// The query itself:
			var copyQuery = Query.Copy(transferMap);
			copyQuery.Where("Id=@id");
			return copyQuery;
		}

		/// <summary>
		/// Sets a particular type with revision handlers. Used via reflection.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="autoService"></param>
		public void SetupForRevisions<T, ID>(AutoService<T, ID> autoService)
			where T : VersionedContent<ID>, new()
			where ID: struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			var contentType = autoService.InstanceType;
			var evtGroup = autoService.EventGroup;

			// Invoked by reflection

			// Create the query:
			var copyQuery = BuildCopyQuery(autoService.InstanceType, autoService.EntityName);
			copyQuery.GetQuery();

			evtGroup.AfterInstanceTypeUpdate.AddEventListener((Context context, AutoService s) => {

				if (s == null)
				{
					return new ValueTask<AutoService>(s);
				}

				var cq = BuildCopyQuery(autoService.InstanceType, autoService.EntityName);
				cq.GetQuery();
				copyQuery = cq;

				return new ValueTask<AutoService>(s);
			});

			// And add an event handler now:
			evtGroup.BeforeUpdate.AddEventListener(async (Context context, T content, T original) =>
			{
				if (content == null)
				{
					return content;
				}

				if (database == null)
				{
					database = Services.Get<MySQLDatabaseService>();
				}

				/*
				// trigger the before create revision events:
				if(beforeUpdateEvent.EventGroup != null){
					beforeUpdateEvent.EventGroup.RevisionBeforeCreate(context, revisionableContent);
				}
				*/

				// Run the copy query now:
				await database.RunWithId(context, copyQuery, content.Id);

				// TODO: Trigger the before and after events (#208):
				// - Requires collecting the ID from the above copy call.
				// - Also requires collecting the EventGroup that the update event came from in order to call the events.

				/*
				// trigger the after create revision events:
				if(beforeUpdateEvent.EventGroup != null){
					// Note: This will not know what the revisions ID is.
					beforeUpdateEvent.EventGroup.RevisionAfterCreate(context, revisionableContent);
				}
				*/

				// Bump its revision number.
				content.Revision++;

				return content;
			}, 11);

			// Add the beforeDelete handler too:
			evtGroup.BeforeDelete.AddEventListener(async (Context context, T content) =>
			{
				if (content == null)
				{
					return content;
				}

				if (database == null)
				{
					database = Services.Get<MySQLDatabaseService>();
				}
				
				/*
				// trigger the before create revision events:
				if(beforeDeleteEvent.EventGroup != null){
					beforeDeleteEvent.EventGroup.RevisionBeforeCreate(context, revisionableContent);
				}
				*/

				// Run the copy query now:
				await database.RunWithId(context, copyQuery, content.Id);

				// TODO: Trigger the before and after events (#208):
				// - Requires collecting the ID from the above copy call.
				// - Also requires collecting the EventGroup that the update event came from in order to call the events.

				/*
				// trigger the after create revision events:
				if(beforeDeleteEvent.EventGroup != null){
					// Note: This will not know what the revisions ID is.
					beforeDeleteEvent.EventGroup.RevisionAfterCreate(context, revisionableContent);
				}
				*/

				return content;
			}, 11);
			
		}

	}
}
