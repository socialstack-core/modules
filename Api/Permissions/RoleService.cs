using System;
using System.Threading.Tasks;
using Api.Contexts;
using System.Collections.Generic;
using Api.Eventing;
using Api.Startup;
using System.Linq;
using Newtonsoft.Json.Linq;
using Api.ColourConsole;

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
					CanViewAdmin = true,
					AdminDashboardJson = "{\"t\":\"Admin/Dashboards/Developer\"}"
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
					var all = await Where(DataOptions.IgnorePermissions).ListAll(ctx);

					var map = new Dictionary<uint, Role>();

					foreach (var role in all)
					{
						map[role.Id] = role;
					}

					// Apply the major roles such as Developer etc:
					Roles.Developer = map[1];
					Roles.Admin = map[2];
					Roles.Guest = map[3];
					Roles.Member = map[4];
					Roles.Banned = map[5];
					Roles.Public = map[6];

					// Construct the default grants:
					await Events.CapabilityOnSetup.Dispatch(ctx, null);

					// Override:
					foreach (var role in all)
					{
						var sameAs = role.InheritedRoleId == 0 ? null : map[role.InheritedRoleId];

						// Load its grant rule JSON:
						SetupGrants(role, sameAs);
					}

				}
			});

			Events.Role.AfterCreate.AddEventListener(async (Context context, Role role) =>
			{
				if (role == null)
				{
					return null;
				}

				Role inheritRole = null;

				if (role.InheritedRoleId != 0)
				{
					// Get the role:
					inheritRole = await Get(context, role.InheritedRoleId, DataOptions.IgnorePermissions);
				}

				SetupGrants(role, inheritRole);

				return role;
			});

			Events.Role.AfterUpdate.AddEventListener(async (Context context, Role role) =>
			{
				if (role == null)
				{
					return null;
				}

				Role inheritRole = null;

				if (role.InheritedRoleId != 0)
				{
					// Get the role:
					inheritRole = await Get(context, role.InheritedRoleId, DataOptions.IgnorePermissions);
				}

				SetupGrants(role, inheritRole);

				return role;
			});

			Events.Role.BeforeGettable.AddEventListener((Context ctx, JsonField<Role, uint> field) => {

				if (field == null)
				{
					return new ValueTask<JsonField<Role, uint>>(field);
				}

				if (field.Name == "AdminDashboardJson" || field.Name == "GrantRuleJson")
				{
					// Not readable if can't view admin.
					field.Readable = (field.ForRole != null && field.ForRole.CanViewAdmin);
				}

				return new ValueTask<JsonField<Role, uint>>(field);
			});
		
			Events.Role.Received.AddEventListener(async (Context context, Role role, int type) =>
			{
				if (role == null)
				{
					return null;
				}

				Role inheritRole = null;

				if (role.InheritedRoleId != 0)
				{
					// Get the role:
					inheritRole = await Get(context, role.InheritedRoleId, DataOptions.IgnorePermissions);
				}

				SetupGrants(role, inheritRole);

				return role;
			});

		}

		/// <summary>
		/// Loads the grant rules from the roles custom JSON.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="grantSameAs"></param>
		private void SetupGrants(Role role, Role grantSameAs)
		{
			if (role == null)
			{
				return;
			}

			// Clear all important grants:
			role.ClearImportantRules();

			if (grantSameAs != null)
			{
				role.GrantTheSameAsImportant(grantSameAs);
			}

			if (string.IsNullOrWhiteSpace(role.GrantRuleJson))
			{
				return;
			}

			try
			{
				var ruleJson = Newtonsoft.Json.JsonConvert.DeserializeObject(role.GrantRuleJson) as JObject;

				// Special case if we have a * - resolve it first:
				if (ruleJson["*"] != null)
				{
					var grantEverythingRule = ruleJson["*"];

					if (grantEverythingRule.Type == JTokenType.Boolean)
					{
						if (grantEverythingRule.Value<bool>())
						{
							role.GrantEverythingImportant();
						}
						else
						{
							role.RevokeEverythingImportant();
						}
					}
				}

				foreach (var kvp in ruleJson)
				{
					// Capability name:
					var capName = kvp.Key.Trim().ToLower();

					if (capName == "*")
					{
						continue;
					}

					// Grant rule:
					var grantRule = kvp.Value;

					if (grantRule.Type == JTokenType.Boolean)
					{
						// True = always granted, false = always denied.
						if (grantRule.Value<bool>())
						{
							role.GrantImportant(capName);
						}
						else
						{
							role.RevokeImportant(capName);
						}
					}
					else if (grantRule.Type == JTokenType.String)
					{
						var grant = grantRule.Value<string>();
						role.If(grant).ThenGrantImportant(capName);
					}
				}
			}
			catch (Exception e)
			{
                WriteColourLine.Warning("[WARN] Unable to parse role JSON for role #" + role.Id + ". Here was the error:" + e.ToString());
			}
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
				var roleIds = idSet.Select(role => role.Id);
				var existingRoles = (await Where("Id=[?]", DataOptions.IgnorePermissions).Bind(roleIds).ListAll(context)).ToDictionary(role => role.Id);
				
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
				var keys = keySet.Select(role => role.Key);
				var existingRoles = (await Where("Key=[?]", DataOptions.IgnorePermissions).Bind(keys).ListAll(context)).ToDictionary(role => role.Key);

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
