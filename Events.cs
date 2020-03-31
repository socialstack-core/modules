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
		/// <summary>
		/// Set of events for a Gallery.
		/// </summary>
		public static EventGroup<Gallery> Gallery;
		
		/// <summary>
		/// Set of events for a GalleryEntry.
		/// </summary>
		public static EventGroup<GalleryEntry> GalleryEntry;
	}

}
