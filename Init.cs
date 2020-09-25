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
			var allPermittedEvents = Events.FindByPlacement(EventPlacement.NotSpecified);

			var listMethodInfo = GetType().GetMethod("SetupForListEvent");
			var standardMethodInfo = GetType().GetMethod("SetupForStandardEvent");

			foreach (var permittedEvent in allPermittedEvents)
			{
				if(permittedEvent.Verb.EndsWith("ed"))
				{
					continue;
				}
				
				// If it had a DontAddPermissions attribute, just skip.
				if (permittedEvent.GetCustomAttribute<DontAddPermissionsAttribute>() != null)
				{
					continue;
				}

				// Create a capability for this event type - e.g. UserCreate becomes a capability called "user_create". Creating it will automatically add it to the set.
				var capability = new Capability(permittedEvent.EntityName + "_" + permittedEvent.Verb);
				capability.ContentType = permittedEvent.PrimaryType;

				if (permittedEvent.Verb == "List")
				{
					// Invoke SetupForListEvent:
					var setupType = listMethodInfo.MakeGenericMethod(new Type[] {
						permittedEvent.PrimaryType
					});

					setupType.Invoke(this, new object[] {
						permittedEvent,
						capability
					});
				}
				else
				{
					// Invoke SetupForStandardEvent:
					var setupType = standardMethodInfo.MakeGenericMethod(new Type[] {
						permittedEvent.PrimaryType
					});

					setupType.Invoke(this, new object[] {
						permittedEvent,
						capability
					});
				}
			}
			
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
				Roles.Public.GrantVerb("load").GrantVerb("list")
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
				Roles.Guest.If((Filter f) => f.IsSelf()).ThenGrantVerb("update", "delete");

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

				return Task.FromResult(source);
			});
		}

		private void SetupPartialRoleRestrictions()
		{
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
		}

		/// <summary>
		/// Sets up a particular list event handler with permissions
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="evtHandler"></param>
		/// <param name="capability"></param>
		public void SetupForListEvent<T>(Api.Eventing.EventHandler evtHandler, Capability capability)
		{
			var handler = evtHandler as EndpointEventHandler<Filter<T>>;

			if (handler == null)
			{
				// We can't mount a permmission handle on this event.
				return;
			}

			// Add an event handler at priority 1 (runs before others).
			handler.AddEventListener((Context context, Filter<T> filter, HttpResponse response) =>
			{
				// Check if the capability is granted.
				// If it is, return the first arg.
				// Otherwise, return null.
				var role = context == null ? Roles.Public : context.Role;

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
		/// <param name="evtHandler"></param>
		/// <param name="capability"></param>
		public void SetupForStandardEvent<T>(Api.Eventing.EventHandler evtHandler, Capability capability)
		{
			var handler = evtHandler as EndpointEventHandler<T>;
			if (handler == null)
			{
				// We can't mount a permmission handle on this event.
				return;
			}

			// Add an event handler at priority 1 (runs before others).
			handler.AddEventListener(async (Context context, T content, HttpResponse response) =>
			{
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
