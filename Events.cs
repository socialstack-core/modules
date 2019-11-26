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
		/// Just before a new tag is created. The given tag won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<Tag> TagBeforeCreate;

		/// <summary>
		/// Just after a tag has been created. The given tag object will now have an ID.
		/// </summary>
		public static EventHandler<Tag> TagAfterCreate;

		/// <summary>
		/// Just before a tag is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<Tag> TagBeforeDelete;

		/// <summary>
		/// Just after a tag has been deleted.
		/// </summary>
		public static EventHandler<Tag> TagAfterDelete;

		/// <summary>
		/// Just before updating a tag. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<Tag> TagBeforeUpdate;

		/// <summary>
		/// Just after updating a tag.
		/// </summary>
		public static EventHandler<Tag> TagAfterUpdate;

		/// <summary>
		/// Just after a tag was loaded.
		/// </summary>
		public static EventHandler<Tag> TagAfterLoad;

		/// <summary>
		/// Just before a service loads a tag list.
		/// </summary>
		public static EventHandler<Filter<Tag>> TagBeforeList;

		/// <summary>
		/// Just after a tag list was loaded.
		/// </summary>
		public static EventHandler<List<Tag>> TagAfterList;

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
