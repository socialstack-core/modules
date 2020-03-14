using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Api.Permissions;
using Api.Results;
using Microsoft.AspNetCore.Mvc;


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
		/// GET /v1/permission/list
		/// Returns meta about the list of available roles and their permission set.
		/// </summary>
		[HttpGet("list")]
		public PermissionInformation List()
		{
			var results = new List<PermissionMeta>();
			
			// For each capability..
			foreach(var capability in Capabilities.All){
				
				var meta = new PermissionMeta()
				{
					Key = capability.Key,
					Description = "Generated capability",
					Grants = new List<GrantMeta>()
				};
				
				// For each role..
				foreach(var role in Roles.All){
					
					// Got it set?
					var rule = role.GetGrantRule(capability.Value);
					
					if(rule != null){
						
						var qry = new StringBuilder();

						// Note that a blank rule description means it's always true.
						// I.e. it's granted and there is no rule around when it is active.
						rule.BuildQuery(qry, 0, false, null);
						
						meta.Grants.Add(new GrantMeta(){
							Role = role,
							RuleDescription = qry.ToString()
						});
					}
					
				}
				
				results.Add(meta);
				
			}
			
            return new PermissionInformation() {
				Capabilities = results,
				Roles = Roles.All
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
		public Role[] Roles;
		
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
