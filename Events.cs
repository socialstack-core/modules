using Api.FeedStories;
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
		/// Just before a new feed story is created. The given feed story won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<FeedStory> FeedStoryBeforeCreate;

		/// <summary>
		/// Just after an feed story has been created. The given feed story object will now have an ID.
		/// </summary>
		public static EventHandler<FeedStory> FeedStoryAfterCreate;

		/// <summary>
		/// Just before an feed story is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<FeedStory> FeedStoryBeforeDelete;

		/// <summary>
		/// Just after an feed story has been deleted.
		/// </summary>
		public static EventHandler<FeedStory> FeedStoryAfterDelete;

		/// <summary>
		/// Just before updating an feed story. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<FeedStory> FeedStoryBeforeUpdate;

		/// <summary>
		/// Just after updating an feed story.
		/// </summary>
		public static EventHandler<FeedStory> FeedStoryAfterUpdate;

		/// <summary>
		/// Just after an feed story was loaded.
		/// </summary>
		public static EventHandler<FeedStory> FeedStoryAfterLoad;

		/// <summary>
		/// Just before a service loads an feed story list.
		/// </summary>
		public static EventHandler<Filter<FeedStory>> FeedStoryBeforeList;

		/// <summary>
		/// Just after an feed story list was loaded.
		/// </summary>
		public static EventHandler<List<FeedStory>> FeedStoryAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new feed story.
		/// </summary>
		public static EndpointEventHandler<FeedStoryAutoForm> FeedStoryCreate;
		/// <summary>
		/// Delete an feed story.
		/// </summary>
		public static EndpointEventHandler<FeedStory> FeedStoryDelete;
		/// <summary>
		/// Update feed story metadata.
		/// </summary>
		public static EndpointEventHandler<FeedStoryAutoForm> FeedStoryUpdate;
		/// <summary>
		/// Load feed story metadata.
		/// </summary>
		public static EndpointEventHandler<FeedStory> FeedStoryLoad;
		/// <summary>
		/// List feed storys.
		/// </summary>
		public static EndpointEventHandler<Filter<FeedStory>> FeedStoryList;

		#endregion

	}

}
