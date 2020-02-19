using Api.Tags;
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
		/// Set of events for a Tag.
		/// </summary>
		public static EventGroup<Tag> Tag;
		
		#endregion

		#region Controller events

		/// <summary>
		/// Create a new tag.
		/// </summary>
		public static EndpointEventHandler<TagAutoForm> TagCreate;
		/// <summary>
		/// Delete a tag.
		/// </summary>
		public static EndpointEventHandler<Tag> TagDelete;
		/// <summary>
		/// Update tag metadata.
		/// </summary>
		public static EndpointEventHandler<TagAutoForm> TagUpdate;
		/// <summary>
		/// Load tag metadata.
		/// </summary>
		public static EndpointEventHandler<Tag> TagLoad;
		/// <summary>
		/// List tags.
		/// </summary>
		public static EndpointEventHandler<Filter<Tag>> TagList;

		#endregion

	}

}
