using Api.Contexts;
using Api.Permissions;
using Api.Startup;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Pages
{
	/// <summary>
	/// Handles pages.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	// Has a raised priority because it's often used by other services to install pages when they start.
	[LoadPriority(5)]
	public partial interface IPageService
    {
		/// <summary>
		/// Delete a page by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a page by its ID.
		/// </summary>
		Task<Page> Get(Context context, int id);

		/// <summary>
		/// Create a new page.
		/// </summary>
		Task<Page> Create(Context context, Page page);

		/// <summary>
		/// Updates the database with the given page data. It must have an ID set.
		/// </summary>
		Task<Page> Update(Context context, Page page);

		/// <summary>
		/// List a filtered set of pages.
		/// </summary>
		/// <returns></returns>
		Task<List<Page>> List(Context context, Filter<Page> filter);
		
		/// <summary>
		/// Installs the given page(s). It checks if they exist by their URL (or ID, if you provide that instead), and if not, creates them.
		/// </summary>
		/// <param name="pages"></param>
		Task Install(params Page[] pages);

		/// <summary>
		/// Installs generic admin pages using the given fields to display on the list page.
		/// </summary>
		/// <param name="typeName"></param>
		/// <param name="fields"></param>
		Task InstallAdminPages(string typeName, string[] fields);
	}
}
