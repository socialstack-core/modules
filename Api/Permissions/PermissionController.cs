using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using Api.Contexts;


namespace Api.Permissions
{
	/// <summary>
	/// Handles an endpoint which describes the permissions on each role.
	/// </summary>
	
	[Route("v1/permission")]
	[ApiController]
	public partial class PermissionController : ControllerBase
    {
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public PermissionController(
		)
        {
        }

		/// <summary>
		/// The cached permission meta.
		/// </summary>
		private PermissionInformation _allPermissionInfo;

		/// <summary>
		/// GET /v1/permission/list
		/// Returns meta about the list of available roles and their permission set.
		/// </summary>
		[HttpGet("list")]
		public async ValueTask<PermissionInformation> List()
		{
			var context = await Request.GetContext();
			
			if(context.Role == null || !context.Role.CanViewAdmin)
			{
				throw PermissionException.Create("permission_list", context);
			}
			
			if (_allPermissionInfo != null)
			{
				return _allPermissionInfo;
			}

			var results = new List<PermissionMeta>();

			// All roles:
			var roles = await Services.Get<RoleService>().Where(DataOptions.IgnorePermissions).ListAll(context);

			// For each capability..
			foreach (var capability in Capabilities.GetAllCurrent())
			{
				var meta = new PermissionMeta()
				{
					Key = capability.Name,
					Description = "Generated capability",
					Grants = new List<GrantMeta>()
				};

				// For each role..
				foreach (var role in roles)
				{

					// Got it set?
					var rule = role.GetGrantRule(capability);

					if (rule != null)
					{
						// Note that a blank rule description means it's always true.
						// I.e. it's granted and there is no rule around when it is active.
						
						meta.Grants.Add(new GrantMeta()
						{
							Role = role,
							RuleDescription = rule.GetQuery()
						});
					}

				}

				results.Add(meta);
			}

			return _allPermissionInfo = new PermissionInformation()
			{
				Capabilities = results,
				Roles = roles
			};
		}
		
    }
	
	/// <summary>
	/// Information about the available permissions.
	/// </summary>
	public class PermissionInformation{
		
		/// <summary>
		/// Information about the available capabilities.
		/// </summary>
		public List<PermissionMeta> Capabilities;
		/// <summary>
		/// The set of available roles.
		/// </summary>
		public List<Role> Roles;
		
	}

	/// <summary>
	/// Meta for a particular role.
	/// </summary>
	public class RoleMeta
	{
		/// <summary>
		/// The role itself.
		/// </summary>
		public Role Role;
		/// <summary>
		/// The list of capabilities that are granted in this role.
		/// </summary>
		public List<CapabilityMeta> Capabilities;
	}

	/// <summary>
	/// Meta for a particular capability.
	/// </summary>
	public class CapabilityMeta
	{
		/// <summary>
		/// The name of the capability, e.g. "user_create".
		/// </summary>
		public string Name;
	}

	/// <summary>
	/// Information about a particular grant.
	/// </summary>
	public class GrantMeta
	{
		/// <summary>The filter description which describes the grant rule as it would appear as an SQL query.</summary>
		public string RuleDescription;
		/// <summary>The role that the grant is on.</summary>
		public Role Role;
	}
	
	/// <summary>
	/// Information for a particular capability, such as which roles have been granted it.
	/// </summary>
	public class PermissionMeta
	{
		/// <summary>
		/// The key of the capability.
		/// </summary>
		public string Key;
		/// <summary>
		/// The description of the capability.
		/// </summary>
		public string Description;
		
		/// <summary>The list of roles which handle this permission in some way.</summary>
		public List<GrantMeta> Grants;
	}
	
}
