using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Categories
{
	/// <summary>
	/// Handles categoryContents.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class CategoryContentService : AutoService<CategoryContent>, ICategoryContentService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public CategoryContentService() : base(Events.CategoryContent)
        {
		}
	}
    
}
