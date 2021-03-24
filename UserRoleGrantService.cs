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
	/// Manages user role grants.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public class UserRoleGrantService : AutoService<UserRoleGrant>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public UserRoleGrantService() : base(Events.UserRoleGrant)
		{
			Cache();
		}
	}
}
