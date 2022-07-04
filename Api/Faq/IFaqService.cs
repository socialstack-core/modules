using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Faqs
{
	/// <summary>
	/// Handles articles - usually seen in e.g. knowledge bases or help guides.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IFaqService
    {
		/// <summary>
		/// Delete a faq by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a faq by its ID.
		/// </summary>
		Task<Faq> Get(Context context, int id);

		/// <summary>
		/// Create a new faq.
		/// </summary>
		Task<Faq> Create(Context context, Faq faq);

		/// <summary>
		/// Updates the database with the given faq data. It must have an ID set.
		/// </summary>
		Task<Faq> Update(Context context, Faq faq);

		/// <summary>
		/// List a filtered set of faq.
		/// </summary>
		/// <returns></returns>
		Task<List<Faq>> List(Context context, Filter<Faq> filter);

	}
}
