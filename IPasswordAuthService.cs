using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.PasswordAuth
{
	/// <summary>
	/// The default password based authentication scheme. Note that other variants of this exist such as one which uses
	/// the same password hash format as Wordpress for easy porting.
	/// You can either add additional schemes or just outright replace this one if you want something else.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IPasswordAuthService
	{
        
	}
}
