using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.AutoForms
{
	/// <summary>
	/// This service drives AutoForm - the form which automatically displays fields in the admin area.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IAutoFormService
	{
		/// <summary>
		/// Lists all available autoforms from this API for a particular role.
		/// </summary>
		/// <returns></returns>
		Task<List<AutoFormInfo>> List(int roleId);
    }
}
