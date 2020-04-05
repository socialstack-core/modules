using System;
using Api.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Api.Eventing;
using Api.Contexts;
using System.Threading.Tasks;

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

			foreach (var permittedEvent in allPermittedEvents)
			{
				if(permittedEvent.Verb == "Created" || permittedEvent.Verb == "Updated" || permittedEvent.Verb == "Deleted"){
					continue;
				}
				
				// Create a capability for this event type - e.g. UserCreate becomes a capability called "user_create". Creating it will automatically add it to the set.
				var capability = new Capability(permittedEvent.EntityName + "_" + permittedEvent.Verb);

				// Next, add an event handler at priority 1 (runs before others).
				permittedEvent.AddEventListener((Context context, object[] args) =>
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
						return null;
					}

					if (args == null || args.Length == 0)
					{
						// No args anyway
						return null;
					}

					if (role.IsGranted(capability, context, args))
					{
						// It's granted - return the first arg:
						return args[0];
					}

					return null;
				}, 1);
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
					Key = "super_admin"
				};

				// Admin - can do almost everything. Usually everything super admin can do, minus system config options/ site level config.
				Roles.Admin = new Role(2)
				{
					Name = "Admin",
					Key = "admin"
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
				
				return Task.FromResult(source);
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
				Roles.Guest.If().IsSelf().ThenGrantVerb("update", "delete");

				// Member - created and (optionally) activated.
				Roles.Member.GrantTheSameAs(Roles.Guest);
				
				return Task.FromResult(source);
			}, 9);

			// After all EventListener's have had a chance to be initialised..
			Events.EventsAfterStart.AddEventListener(async (Context ctx, object[] args) =>
			{
				// Trigger RoleSetup:
				await Events.RoleOnSetup.Dispatch(ctx, null);

				// Trigger capability setup:
				await Events.CapabilityOnSetup.Dispatch(ctx, null);

				return args == null || args.Length == 0 ? null : args[0];
			});
		}

	}
}
