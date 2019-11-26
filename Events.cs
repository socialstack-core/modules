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
		/// Just before a new category is created. The given category won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<Category> CategoryBeforeCreate;

		/// <summary>
		/// Just after an category has been created. The given category object will now have an ID.
		/// </summary>
		public static EventHandler<Category> CategoryAfterCreate;

		/// <summary>
		/// Just before an category is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<Category> CategoryBeforeDelete;

		/// <summary>
		/// Just after an category has been deleted.
		/// </summary>
		public static EventHandler<Category> CategoryAfterDelete;

		/// <summary>
		/// Just before updating an category. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<Category> CategoryBeforeUpdate;

		/// <summary>
		/// Just after updating an category.
		/// </summary>
		public static EventHandler<Category> CategoryAfterUpdate;

		/// <summary>
		/// Just after an category was loaded.
		/// </summary>
		public static EventHandler<Category> CategoryAfterLoad;

		/// <summary>
		/// Just before a service loads an category list.
		/// </summary>
		public static EventHandler<Filter<Category>> CategoryBeforeList;

		/// <summary>
		/// Just after an category list was loaded.
		/// </summary>
		public static EventHandler<List<Category>> CategoryAfterList;

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
