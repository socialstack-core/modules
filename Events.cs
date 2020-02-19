using Api.Categories;
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
		/// Set of events for a Category.
		/// </summary>
		public static EventGroup<Category> Category;
		
		#endregion

		#region Controller events

		/// <summary>
		/// Create a new category.
		/// </summary>
		public static EndpointEventHandler<CategoryAutoForm> CategoryCreate;
		/// <summary>
		/// Delete an category.
		/// </summary>
		public static EndpointEventHandler<Category> CategoryDelete;
		/// <summary>
		/// Update category metadata.
		/// </summary>
		public static EndpointEventHandler<CategoryAutoForm> CategoryUpdate;
		/// <summary>
		/// Load category metadata.
		/// </summary>
		public static EndpointEventHandler<Category> CategoryLoad;
		/// <summary>
		/// List categories.
		/// </summary>
		public static EndpointEventHandler<Filter<Category>> CategoryList;

		#endregion

	}

}
