using System;
using System.Threading.Tasks;
using Api.Database;
using Api.Emails;
using Microsoft.AspNetCore.Http;
using Api.Contexts;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using System.Collections;
using System.Reflection;
using Api.Startup;
using System.Linq;

namespace Api.Permissions
{

	/// <summary>
	/// Manages user roles.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public class UserRoleService : AutoService<UserRole>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public UserRoleService() : base(Events.UserRole)
		{
			
			// Install the admin pages.
			InstallAdminPages("Roles", "fa:fa-user-lock", new string[] { "id", "key", "name" });

			// Core roles that must be installed always:
			Install(
				// Developer (Super admin) - can do everything.
				new UserRole()
				{
					Id = 1,
					Name = "Developer",
					Key = "developer",
					CanViewAdmin = true
					// Todo: Admin homepage displays e.g. error metrics
				},
				
				// Admin - can do almost everything. Usually everything super admin can do, minus system config options/ site level config.
				new UserRole()
				{
					Id = 2,
					Name = "Admin",
					Key = "admin",
					CanViewAdmin = true
					// Todo: Admin homepage displays e.g. traffic metrics
				},

				// Guest - account created, not activated. Basically the same as a public account by default.
				new UserRole()
				{
					Id = 3,
					Name = "Guest",
					Key = "guest"
				}, // <-- In this case, we grant the same as public.

				// Member - created and (optionally) activated.
				new UserRole()
				{
					Id = 4,
					Name = "Member",
					Key = "member"
				},
				
				// Banned role - can do basically nothing.
				new UserRole()
				{
					Id = 5,
					Name = "Banned",
					Key = "banned"
				},

				// Public - the role used by anonymous users.
				new UserRole()
				{
					Id = 6,
					Name = "Public",
					Key = "public"
				}
			);
			
			Cache();
		}
		
		/// <summary>
		/// Installs the given role(s). It checks if they exist by their key or ID, and if not, creates them.
		/// </summary>
		/// <param name="roles"></param>
		public void Install(params UserRole[] roles)
		{
			if (Services.Started)
			{
				Task.Run(async () =>
				{
					await InstallNow(roles);
				});
			}
			else
			{
				Events.Service.AfterStart.AddEventListener(async (Context ctx, object src) =>
				{
					await InstallNow(roles);
					return src;
				});
			}
		}
		
		/// <summary>
		/// Installs the given role(s). It checks if they exist by their key or ID, and if not, creates them.
		/// </summary>
		/// <param name="roles"></param>
		public async ValueTask InstallNow(params UserRole[] roles)
		{
			var context = new Context();

			// Get the set of roles which we'll match by ID:
			var idSet = roles.Where(role => role.Id != 0 || role.Key == "public");

			if (idSet.Any())
			{
				// Get the roles:
				var filter = new Filter<UserRole>();
				filter.Id(idSet.Select(role => role.Id));
				var existingRoles = (await ListNoCache(context, filter, false, DataOptions.IgnorePermissions)).ToDictionary(role => role.Id);
				
				// For each to consider for install..
				foreach (var role in idSet)
				{
					// If it doesn't already exist, create it.
					if (!existingRoles.ContainsKey(role.Id))
					{
						await Create(context, role, DataOptions.IgnorePermissions);
					}
				}
			}
			
			// Get the set of roles which we'll match by key:
			var keySet = roles.Where(role => role.Id == 0 && role.Key != "public");

			if (keySet.Any())
			{
				// Get the roles by those keys:
				var filter = new Filter<UserRole>();
				filter.EqualsSet("Key", keySet.Select(role => role.Key));
				var existingRoles = (await ListNoCache(context, filter, false, DataOptions.IgnorePermissions)).ToDictionary(role => role.Key);

				// For each role to consider for install..
				foreach (var role in keySet)
				{
					// If it doesn't already exist, create it.
					if (!existingRoles.ContainsKey(role.Key))
					{
						await Create(context, role, DataOptions.IgnorePermissions);
					}
				}
			}
		}
		
	}
}
