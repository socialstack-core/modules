using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Views
{
	/// <summary>
	/// Handles views.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IViewService
    {

		/// <summary>
		/// Deletes a view by its ID.
		/// </summary>
		Task<bool> Delete(Context context, int viewId);

		/// <summary>
		/// Gets a single view by its ID.
		/// </summary>
		Task<View> Get(Context context, int viewId);

		/// <summary>
		/// Creates a new view.
		/// </summary>
		Task<View> Create(Context context, View view);

		/// <summary>
		/// Updates the given view.
		/// </summary>
		Task<View> Update(Context context, View view);

		/// <summary>
		/// List a filtered set of views.
		/// </summary>
		/// <returns></returns>
		Task<List<View>> List(Context context, Filter<View> filter);
		
		/// <summary>
		/// Marks a content item of the given type as viewed.
		/// </summary>
		Task MarkViewed(Context context, int contentTypeId, int id);
		
	}
}
