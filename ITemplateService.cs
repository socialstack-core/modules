using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Templates
{
	/// <summary>
	/// Handles templates.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface ITemplateService
    {
		/// <summary>
		/// Delete a template by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a template by its ID.
		/// </summary>
		Task<Template> Get(Context context, int id);

		/// <summary>
		/// Create a new template.
		/// </summary>
		Task<Template> Create(Context context, Template template);

		/// <summary>
		/// Updates the database with the given template data. It must have an ID set.
		/// </summary>
		Task<Template> Update(Context context, Template template);

		/// <summary>
		/// List a filtered set of pages.
		/// </summary>
		/// <returns></returns>
		Task<List<Template>> List(Context context, Filter<Template> filter);
	}
}
