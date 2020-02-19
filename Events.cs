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
		/// Just after an article list was loaded.
		/// </summary>
		public static EventGroup<Article> Article;
		
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
