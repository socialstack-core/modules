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
	[LoadPriority(4)]
	public class RoleService : AutoService<Role>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public RoleService() : base(Events.Role)
		{
			// Install the admin pages.
			InstallAdminPages("Roles", "fa:fa-user-lock", new string[] { "id", "key", "name" });

			// Core roles that must be installed always:
			Install(
				// Developer (Super admin) - can do everything.
				new Role()
				{
					Id = 1,
					Name = "Developer",
					Key = "developer",
					CanViewAdmin = true
					// Todo: Admin homepage displays e.g. error metrics
				},
				
				// Admin - can do almost everything. Usually everything super admin can do, minus system config options/ site level config.
				new Role()
				{
					Id = 2,
					Name = "Admin",
					Key = "admin",
					CanViewAdmin = true
					// Todo: Admin homepage displays e.g. traffic metrics
				},

				// Guest - account created, not activated. Largely the same as a public account by default.
				new Role()
				{
					Id = 3,
					Name = "Guest",
					Key = "guest"
				},

				// Member - created and (optionally) activated.
				new Role()
				{
					Id = 4,
					Name = "Member",
					Key = "member"
				},

				// Banned role - can do basically nothing.
				new Role()
				{
					Id = 5,
					Name = "Banned",
					Key = "banned"
				},

				// Public - the role used by anonymous users. 0 aliases to 6.
				new Role()
				{
					Id = 6,
					Name = "Public",
					Key = "public"
				}
			);

			Cache(new CacheConfig<Role>()
			{
				LowFrequencySequentialIds = true,
				OnCacheLoaded = async () => {
					started = true;

					if (_toInstall != null)
					{
						// Install now:
						await InstallNow(_toInstall);
						_toInstall = null;
					}

					// Setup grant rules.
					var ctx = new Context();
					var all = await List(ctx, null, DataOptions.IgnorePermissions);

					var map = new Dictionary<uint, Role>();

					foreach (var role in all)
					{
						map[role.Id] = role;
					}

					foreach (var role in all)
					{
						if (role.InheritedRoleId != 0)
						{
							role.GrantTheSameAs(map[role.InheritedRoleId]);
						}
					}

					// Apply the major roles such as Developer etc:
					Roles.Developer = map[1];
					Roles.Admin = map[2];
					Roles.Guest = map[3];
					Roles.Member = map[4];
					// Banned = Role 5
					Roles.Public = map[6];

					// Construct the default grants:
					await Events.CapabilityOnSetup.Dispatch(ctx, null);
				}
			});

		}

		private bool started;
		private List<Role> _toInstall;

		/// <summary>
		/// Installs the given role(s). It checks if they exist by their key or ID, and if not, creates them.
		/// </summary>
		/// <param name="roles"></param>
		public void Install(params Role[] roles)
		{
			if (started)
			{
				Task.Run(async () =>
				{
					await InstallNow(roles);
				});
			}
			else
			{
				if (_toInstall == null)
				{
					_toInstall = new List<Role>();
				}

				_toInstall.AddRange(roles);
			}
		}
		
		/// <summary>
		/// Installs the given role(s). It checks if they exist by their key or ID, and if not, creates them.
		/// </summary>
		/// <param name="roles"></param>
		public async ValueTask InstallNow(IEnumerable<Role> roles)
		{
			var context = new Context();

			// Get the set of roles which we'll match by ID:
			var idSet = roles.Where(role => role.Id != 0 || role.Key == "public");

			if (idSet.Any())
			{
				// Get the roles:
				var filter = new Filter<Role>();
				filter.Id(idSet.Select(role => role.Id));
				var existingRoles = (await List(context, filter, DataOptions.IgnorePermissions)).ToDictionary(role => role.Id);
				
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
				var filter = new Filter<Role>();
				filter.EqualsSet("Key", keySet.Select(role => role.Key));
				var existingRoles = (await List(context, filter, DataOptions.IgnorePermissions)).ToDictionary(role => role.Key);

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
