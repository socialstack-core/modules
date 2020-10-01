using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;

namespace Api.Articles
{
	/// <summary>
	/// Handles articles - containers for individual article posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ArticleService : AutoService<Article>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ArticleService() : base(Events.Article) {
			
			InstallAdminPages("Articles", "fa:fa-book-open", new string[] { "id", "name" });
			
		}
	
	}
    
}
