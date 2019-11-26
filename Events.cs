using Api.Articles;
using Api.Permissions;
using System.Collections.Generic;

namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{

		#region Service events

		/// <summary>
		/// Just before a new article is created. The given article won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<Article> ArticleBeforeCreate;

		/// <summary>
		/// Just after an article has been created. The given article object will now have an ID.
		/// </summary>
		public static EventHandler<Article> ArticleAfterCreate;

		/// <summary>
		/// Just before an article is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<Article> ArticleBeforeDelete;

		/// <summary>
		/// Just after an article has been deleted.
		/// </summary>
		public static EventHandler<Article> ArticleAfterDelete;

		/// <summary>
		/// Just before updating an article. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<Article> ArticleBeforeUpdate;

		/// <summary>
		/// Just after updating an article.
		/// </summary>
		public static EventHandler<Article> ArticleAfterUpdate;

		/// <summary>
		/// Just after an article was loaded.
		/// </summary>
		public static EventHandler<Article> ArticleAfterLoad;

		/// <summary>
		/// Just before a service loads an article list.
		/// </summary>
		public static EventHandler<Filter<Article>> ArticleBeforeList;

		/// <summary>
		/// Just after an article list was loaded.
		/// </summary>
		public static EventHandler<List<Article>> ArticleAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new article.
		/// </summary>
		public static EndpointEventHandler<ArticleAutoForm> ArticleCreate;
		/// <summary>
		/// Delete an article.
		/// </summary>
		public static EndpointEventHandler<Article> ArticleDelete;
		/// <summary>
		/// Update article metadata.
		/// </summary>
		public static EndpointEventHandler<ArticleAutoForm> ArticleUpdate;
		/// <summary>
		/// Load article metadata.
		/// </summary>
		public static EndpointEventHandler<Article> ArticleLoad;
		/// <summary>
		/// List articles.
		/// </summary>
		public static EndpointEventHandler<Filter<Article>> ArticleList;

		#endregion

	}

}
