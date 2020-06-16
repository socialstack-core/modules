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
		/// <summary>
		/// Set of events for tag content.
		/// </summary>
		public static EventGroup<TagContent> TagContent;
		
		/// <summary>
		/// Set of events for a Tag.
		/// </summary>
		public static EventGroup<Tag> Tag;
	}
}