using Api.Galleries;
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
		/// Just before a new gallery is created. The given gallery won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<Gallery> GalleryBeforeCreate;

		/// <summary>
		/// Just after an gallery has been created. The given gallery object will now have an ID.
		/// </summary>
		public static EventHandler<Gallery> GalleryAfterCreate;

		/// <summary>
		/// Just before an gallery is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<Gallery> GalleryBeforeDelete;

		/// <summary>
		/// Just after an gallery has been deleted.
		/// </summary>
		public static EventHandler<Gallery> GalleryAfterDelete;

		/// <summary>
		/// Just before updating an gallery. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<Gallery> GalleryBeforeUpdate;

		/// <summary>
		/// Just after updating an gallery.
		/// </summary>
		public static EventHandler<Gallery> GalleryAfterUpdate;

		/// <summary>
		/// Just after an gallery was loaded.
		/// </summary>
		public static EventHandler<Gallery> GalleryAfterLoad;

		/// <summary>
		/// Just before a service loads an gallery list.
		/// </summary>
		public static EventHandler<Filter<Gallery>> GalleryBeforeList;

		/// <summary>
		/// Just after an gallery list was loaded.
		/// </summary>
		public static EventHandler<List<Gallery>> GalleryAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new gallery.
		/// </summary>
		public static EndpointEventHandler<GalleryAutoForm> GalleryCreate;
		/// <summary>
		/// Delete an gallery.
		/// </summary>
		public static EndpointEventHandler<Gallery> GalleryDelete;
		/// <summary>
		/// Update gallery metadata.
		/// </summary>
		public static EndpointEventHandler<GalleryAutoForm> GalleryUpdate;
		/// <summary>
		/// Load gallery metadata.
		/// </summary>
		public static EndpointEventHandler<Gallery> GalleryLoad;
		/// <summary>
		/// List gallerys.
		/// </summary>
		public static EndpointEventHandler<Filter<Gallery>> GalleryList;

		#endregion

	}

}
