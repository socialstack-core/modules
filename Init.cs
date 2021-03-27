using System;
using Api.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Api.Eventing;
using Api.Contexts;
using System.Threading.Tasks;
using Api.Permissions;
using Api.Database;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Api.Users;
using Newtonsoft.Json.Linq;

namespace Api.Permissions
{

	/// <summary>
	/// Instances capabilities during the very earliest phases of startup.
	/// </summary>
	[EventListener]
	public class Init
	{

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public Init()
		{

			var setupForTypeMethod = GetType().GetMethod(nameof(SetupForType));
			
			Events.Service.AfterCreate.AddEventListener((Context context, AutoService service) => {

				if (service == null)
				{
					return new ValueTask<AutoService>(service);
				}

				// Get the content type for this service and event group:
				var contentType = service.ServicedType;

				if (contentType == null)
				{
					return new ValueTask<AutoService>(service);
				}
				
				// Get its event group so we can add permission handlers:
				var eventGroup = service.GetEventGroup();

				if (eventGroup == null)
				{
					return new ValueTask<AutoService>(service);
				}

				var idType = contentType.GetMethod("GetId").ReturnType;

				// Add List event:
				var setupType = setupForTypeMethod.MakeGenericMethod(new Type[] {
					contentType,
					idType
				});

				setupType.Invoke(this, new object[] {
					eventGroup
				});

				return new ValueTask<AutoService>(service);
			}, 1);

			// Hook the default role setup. It's done like this so it can be removed by a plugin if wanted.
			Events.RoleOnSetup.AddEventListener((Context context, object source) => {
				
				// Public - the role used by anonymous users.
				Roles.Public = new Role(0)
				{
					Name = "Public",
					Key = "public"
				};
				
				// Super admin - can do everything.
				// Note that you can grant everything and then revoke certain things if you want.
				Roles.SuperAdmin = new Role(1)
				{
					Name = "Super Admin",
					Key = "super_admin",
					CanViewAdmin = true
				};

				// Admin - can do almost everything. Usually everything super admin can do, minus system config options/ site level config.
				Roles.Admin = new Role(2)
				{
					Name = "Admin",
					Key = "admin",
					CanViewAdmin = true
				}; // <-- In this case, we grant the same as SA.

				// Guest - account created, not activated. Basically the same as a public account by default.
				Roles.Guest = new Role(3)
				{
					Name = "Guest",
					Key = "guest"
				}; // <-- In this case, we grant the same as public.

				// Member - created and (optionally) activated.
				Roles.Member = new Role(4)
				{
					Name = "Member",
					Key = "member"
				};
				
				// Banned role - can do basically nothing.
				Roles.Banned = new Role(5)
				{
					Name = "Banned",
					Key = "banned"
				};

				return new ValueTask<object>(source);
			}, 9);
			
			// Hook the default role setup. It's done like this so it can be removed by a plugin if wanted.
			Events.CapabilityOnSetup.AddEventListener((Context context, object source) => {

				// Public - the role used by anonymous users.
				Roles.Public.GrantFeature("load").GrantFeature("list")
					.Revoke("user_load").Revoke("user_list")
					.Grant("user_create");
				
				// Super admin - can do everything.
				// Note that you can grant everything and then revoke certain things if you want.
				Roles.SuperAdmin.GrantEverything();

				// Admin - can do almost everything. Usually everything super admin can do, minus system config options/ site level config.
				Roles.Admin.GrantTheSameAs(Roles.SuperAdmin); // <-- In this case, we grant the same as SA.

				// Guest - account created, not activated. Basically the same as a public account by default.
				Roles.Guest.GrantTheSameAs(Roles.Public); // <-- In this case, we grant the same as public.

				// Users can update or delete any content they've created themselves:
				Roles.Guest.If((Filter f) => f.IsSelf()).ThenGrantFeature("update", "delete");

				// Member - created and (optionally) activated.
				Roles.Member.GrantTheSameAs(Roles.Guest);

				return new ValueTask<object>(source);
			}, 9);

			// After all EventListener's have had a chance to be initialised..
			Events.EventsAfterStart.AddEventListener(async (Context ctx, object source) =>
			{
				// Trigger RoleSetup:
				await Events.RoleOnSetup.Dispatch(ctx, null);

				// Trigger capability setup:
				await Events.CapabilityOnSetup.Dispatch(ctx, null);

				// Now all the roles and caps have been setup, inject role restrictions:
				SetupPartialRoleRestrictions();

				// Setup IHaveUserRestrictions too:
				SetupUserRestrictions();

				return Task.FromResult(source);
			});
		}

		/// <summary>
		/// Invoked by reflection. Adds user restrictions to the given event group for a particular content type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public void SetupUserRestrictionsForContent<T>() where T: IHaveId<int>, IHaveUserRestrictions
		{
			var eventGroup = Events.GetGroup<T>();
			PermittedContentService permits = null;
			UserService users = null;

			var contentTypeId = ContentTypes.GetId(typeof(T));
			var userContentTypeId = ContentTypes.GetId(typeof(User));

			// After load, potentially hook the list of permitted users.
			eventGroup.AfterLoad.AddEventListener(async (Context context, T content) => {

				if (content == null)
				{
					return content;
				}

				if (content.PermittedUsersListVisible)
				{

					if (permits == null)
					{
						permits = Services.Get<PermittedContentService>();
					}

					// Load the permitted users list:
					content.PermittedUsers = await permits.List(context, new Filter<PermittedContent>().Equals("ContentId", content.GetId()).And().Equals("ContentTypeId", contentTypeId), DataOptions.IgnorePermissions);

				}

				return content;
			});

			// After list, potentially hook the list of permitted users.
			eventGroup.AfterList.AddEventListener(async (Context context, List<T> set) => {

				if (set == null)
				{
					return set;
				}

				foreach (var content in set)
				{
					if (content.PermittedUsersListVisible)
					{

						if (permits == null)
						{
							permits = Services.Get<PermittedContentService>();
						}

						// Load the permitted users list. These are cached so we're not hitting the database here.
						content.PermittedUsers = await permits.List(context, new Filter<PermittedContent>().Equals("ContentId", content.GetId()).And().Equals("ContentTypeId", contentTypeId), DataOptions.IgnorePermissions);

					}
				}

				return set;
			});

			// Hook up a MultiSelect on the underlying fields:
			eventGroup.BeforeSettable.AddEventListener((Context ctxbset, JsonField<T> field) =>
			{
				if (field != null && field.Name == "PermittedUsers")
				{
					field.Module = null;

					// Defer the set after the ID is available:
					field.AfterId = true;

					// On set, convert provided IDs into tag objects.
					field.OnSetValue.AddEventListener(async (Context context, object value, T targetObject, JToken srcToken) =>
					{
						// The value should be an array of ints.
						if (value is not JArray permitArray)
						{
							return null;
						}

						var permitList = new List<PermittedContent>();

						// Anon can't make self-permits
						var createSelfPermit = context.UserId != 0;

						if (users == null)
						{
							users = Services.Get<UserService>();
						}

						var now = DateTime.UtcNow;

						if (permits == null)
						{
							permits = Services.Get<PermittedContentService>();
						}
						
						foreach (var token in permitArray)
						{
							// Token can be either a user ID, or {contentTypeId: x, contentId: y}, or {contentId: y} or {userId: y}

							var permitToCreate = new PermittedContent();

							if (token.Type == JTokenType.Integer)
							{
								permitToCreate.PermittedContentId = token.Value<int>();
								permitToCreate.PermittedContentTypeId = userContentTypeId;
							}
							else if (token.Type == JTokenType.Object)
							{
								var jObj = token as JObject;
								
								if (jObj.TryGetValue("userId", out JToken v))
								{
									permitToCreate.PermittedContentId = v.Value<int>();
									permitToCreate.PermittedContentTypeId = userContentTypeId;
								}
								else if (jObj.TryGetValue("contentId", out v))
								{
									permitToCreate.PermittedContentId = v.Value<int>();

									if (jObj.TryGetValue("contentTypeId", out v))
									{
										permitToCreate.PermittedContentTypeId = v.Value<int>();
									}
									else
									{
										permitToCreate.PermittedContentTypeId = userContentTypeId;
									}
								}
								else
								{
									continue;
								}

							}
							else
							{
								continue;
							}

							permitToCreate.UserId = context.UserId;
							permitToCreate.ContentId = targetObject.GetId();
							permitToCreate.ContentTypeId = contentTypeId;
							permitToCreate.CreatedUtc = now;

							// Get the targeted content, with a permission check, 
							// just in case somebody attempts to permit something they aren't allowed to see.
							var permittedContent = await Content.Get(context, permitToCreate.PermittedContentTypeId, permitToCreate.PermittedContentId, true);

							if (permittedContent == null)
							{
								continue;
							}

							if (context.HasContent(permitToCreate.PermittedContentTypeId, permitToCreate.PermittedContentId))
							{
								// This one is for "self" so don't create it.
								createSelfPermit = false;
							}

							permitList.Add(permitToCreate);
							permitToCreate.Permitted = permittedContent;
							await permits.Create(context, permitToCreate);

						}

						if (createSelfPermit)
						{
							// Create a permit for "me".
							var myProfile = users.GetProfile(await context.GetUser());

							var permitToCreate = new PermittedContent()
							{
								UserId = context.UserId,
								ContentId = targetObject.GetId(),
								ContentTypeId = contentTypeId,
								CreatedUtc = now,
								Permitted = myProfile,
								AcceptedUtc = now,
								PermittedContentId = context.UserId,
								PermittedContentTypeId = userContentTypeId
							};

							await permits.Create(context, permitToCreate);
							permitList.Add(permitToCreate);
						}

						return permitList;
					});

				}

				return new ValueTask<JsonField<T>>(field);
			});

			// Permission blocks next. Ensure we only return results for things that this user is permitted to see.
			// We'll do this by extending the grant filter.

			#warning todo revisit permits.

			/*
			foreach (var role in Roles.All)
			{
				// Note that this applies to everybody - admins included.

				var typeName = typeof(T).Name.ToLower();

				if (Capabilities.All.TryGetValue(typeName + "_load", out Capability loadCapability))
				{
					role.AddHasPermit(loadCapability, typeof(T), contentTypeId);
				}

				if (Capabilities.All.TryGetValue(typeName + "_list", out Capability listCapability))
				{
					role.AddHasPermit(listCapability, typeof(T), contentTypeId);
				}

			}
			*/
		}

		private static void SetupPartialRoleRestrictions()
		{
#warning todo revisit partial role restrictions
			/*
			// For each type, build the field map of roles that it wants to partially restrict (if any):
			foreach (var kvp in ContentTypes.TypeMap)
			{
				var contentType = kvp.Value;

				if (!typeof(IHaveRoleRestrictions).IsAssignableFrom(contentType))
				{
					continue;
				}

				// This type has partial role restrictions.
				// This means it would like to restrict some content (but not all) by role.
				foreach (var role in Roles.All)
				{

					// Is there a field called VisibleToRoleX?
					var fieldName = "VisibleToRole" + role.Id;
					var field = contentType.GetField(fieldName);

					if (field == null)
					{
						continue;
					}

					// Ok - this type has visibility which varies within a role.
					// For example, members of the public see certain pages but not e.g. the admin pages.

					// Next, we'll inject into the permission filter a restriction on this field.
					// I.e. the field must be true to go ahead.
					var typeName = contentType.Name.ToLower();

					if (Capabilities.All.TryGetValue(typeName + "_load", out Capability loadCapability))
					{
						role.AddRoleRestrictionToFilter(loadCapability, contentType, fieldName);
					}

					if (Capabilities.All.TryGetValue(typeName + "_list", out Capability listCapability))
					{
						role.AddRoleRestrictionToFilter(listCapability, contentType, fieldName);
					}

				}

			}
			*/
		}

		private void SetupUserRestrictions()
		{
			// For each type, add the user restriction handlers if the type requires them:
			var userRestrictMethodInfo = GetType().GetMethod("SetupUserRestrictionsForContent");

			foreach (var kvp in ContentTypes.TypeMap)
			{
				var contentType = kvp.Value;

				if (!typeof(IHaveUserRestrictions).IsAssignableFrom(contentType))
				{
					continue;
				}

				// Invoke:
				var setupType = userRestrictMethodInfo.MakeGenericMethod(new Type[] {
					contentType
				});

				setupType.Invoke(this, Array.Empty<object>());
			}
		}

		/// <summary>
		/// Sets up for the given type with its event group.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="group"></param>
		public void SetupForType<T, ID>(EventGroup<T, ID> group)
		{

			var fields = group.GetType().GetFields();

			group.AllWithCapabilities = null;

			foreach (var field in fields)
			{
				if (field.FieldType == typeof(Eventing.EventHandler<T>) && field.Name.StartsWith("Before"))
				{
					// Standard event handle.
					var eventHandler = field.GetValue(group) as Eventing.EventHandler<T>;

					// Create a capability for this event type - e.g. User.BeforeCreate becomes a capability called "User_Create".
					if (eventHandler.Capability == null)
					{
						var capability = new Capability(typeof(T), field.Name[6..]);
						eventHandler.Capability = capability;
					}

					if (group.AllWithCapabilities == null)
					{
						group.AllWithCapabilities = new List<Api.Eventing.EventHandler>();
					}

					group.AllWithCapabilities.Add(eventHandler);

					SetupForStandardEvent<T>(eventHandler, eventHandler.Capability);
				}
				else if (field.FieldType == typeof(Eventing.EventHandler<T>) && field.Name.StartsWith("After") && field.Name.EndsWith("Load"))
				{
					// Special case for Load events because BeforeLoad is an EventHandler with just an ID and we can't use that to figure out if the load is ok or not.
					var eventHandler = field.GetValue(group) as Eventing.EventHandler<T>;

					// Create a capability for this event type - e.g. User.AfterLoad becomes a capability called "User_Load".

					if (eventHandler.Capability == null)
					{
						var capability = new Capability(typeof(T), field.Name[5..]);
						eventHandler.Capability = capability;
					}

					if (group.AllWithCapabilities == null)
					{
						group.AllWithCapabilities = new List<Api.Eventing.EventHandler>();
					}

					group.AllWithCapabilities.Add(eventHandler);

					SetupForStandardEvent<T>(eventHandler, eventHandler.Capability);
				}
				else if (field.FieldType == typeof(Eventing.EventHandler<Filter<T>>) && field.Name.StartsWith("Before"))
				{
					// List handle
					var eventHandler = field.GetValue(group) as Eventing.EventHandler<Filter<T>>;

					// Create a capability for this event type - e.g. User.BeforeCreate becomes a capability called "User_Create".

					if (eventHandler.Capability == null)
					{
						var capability = new Capability(typeof(T), field.Name[6..]);
						eventHandler.Capability = capability;
					}

					if (group.AllWithCapabilities == null)
					{
						group.AllWithCapabilities = new List<Api.Eventing.EventHandler>();
					}

					group.AllWithCapabilities.Add(eventHandler);

					SetupForListEvent<T>(eventHandler, eventHandler.Capability);
				}

			}

		}

		/// <summary>
		/// Adds the given capability to all currently loaded roles.
		/// </summary>
		/// <param name="capability"></param>
		private static void CapabilityCreated(Capability capability)
		{
			foreach (var role in Roles.All)
			{
				role.AddCapability(capability);
			}
		}

		/// <summary>
		/// Sets up a particular Before*List event handler with permissions
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="handler"></param>
		/// <param name="capability"></param>
		public void SetupForListEvent<T>(Api.Eventing.EventHandler<Filter<T>> handler, Capability capability)
		{
			// Indicate to the roles that the cap exists:
			CapabilityCreated(capability);

			// Add an event handler at priority 1 (runs before others).
			handler.AddEventListener((Context context, Filter<T> filter) =>
			{
				if (context.IgnorePermissions)
				{
					return new ValueTask<Filter<T>>(filter);
				}
				
				// Check if the capability is granted.
				// If it is, return the first arg.
				// Otherwise, return null.
				var role = context.Role;

				if (role == null)
				{
					// No user role - can't grant this capability.
					// This is likely to indicate a deeper issue, so we'll warn about it:
					Console.WriteLine("Warning: User ID " + context.UserId + " has no role (or the role with that ID hasn't been instanced).");
					throw PermissionException.Create(capability.Name, context);
				}

				// Get the grant rule (a filter) for this role + capability:
				var rawGrantRule = role.GetGrantRule(capability);
				var srcFilter = role.GetSourceFilter(capability);

				// If it's outright rejected..
				if (rawGrantRule == null)
				{
					throw PermissionException.Create(capability.Name, context);
				}

				// Otherwise, merge the user filter with the one from the grant system (if we need to).
				// Special case for the common true always node:
				if (rawGrantRule is FilterTrue)
				{
					return new ValueTask<Filter<T>>(filter);
				}

				if (filter == null)
				{
					// All permission handled List calls require a filter.
					throw PermissionException.Create("Internal issue: A filter is required", context);
				}

				// Both are set. Must combine them safely:
				return new ValueTask<Filter<T>>(filter.Combine(rawGrantRule, srcFilter?.ParamValueResolvers) as Filter<T>);
			}, 1);
		}

		/// <summary>
		/// Sets up a particular non-list event handler with permissions
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="handler"></param>
		/// <param name="capability"></param>
		public void SetupForStandardEvent<T>(Api.Eventing.EventHandler<T> handler, Capability capability)
		{
			// Indicate to the roles that the cap exists:
			CapabilityCreated(capability);

			// Add an event handler at priority 1 (runs before others).
			handler.AddEventListener(async (Context context, T content) =>
			{
				if (context.IgnorePermissions)
				{
					return content;
				}

				// Check if the capability is granted.
				// If it is, return the first arg.
				// Otherwise, return null.
				var role = context == null ? Roles.Public : context.Role;

				if (role == null)
				{
					// No user role - can't grant this capability.
					// This is likely to indicate a deeper issue, so we'll warn about it:
					throw PermissionException.Create(capability.Name, context, "No role");
				}

				if (await role.IsGranted(capability, context, content))
				{
					// It's granted - return the first arg:
					return content;
				}

				throw PermissionException.Create(capability.Name, context);
			}, 1);
		}

	}
}
