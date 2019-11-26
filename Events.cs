using Api.Reactions;
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
		/// Just before a new reaction is created. The given reaction won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<Reaction> ReactionBeforeCreate;

		/// <summary>
		/// Just after a reaction has been created. The given reaction object will now have an ID.
		/// </summary>
		public static EventHandler<Reaction> ReactionAfterCreate;

		/// <summary>
		/// Just before a reaction is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<Reaction> ReactionBeforeDelete;

		/// <summary>
		/// Just after a reaction has been deleted.
		/// </summary>
		public static EventHandler<Reaction> ReactionAfterDelete;

		/// <summary>
		/// Just before updating a reaction. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<Reaction> ReactionBeforeUpdate;

		/// <summary>
		/// Just after updating a reaction.
		/// </summary>
		public static EventHandler<Reaction> ReactionAfterUpdate;

		/// <summary>
		/// Just after a reaction was loaded.
		/// </summary>
		public static EventHandler<Reaction> ReactionAfterLoad;

		/// <summary>
		/// Just before a service loads a reaction list.
		/// </summary>
		public static EventHandler<Filter<Reaction>> ReactionBeforeList;

		/// <summary>
		/// Just after a reaction list was loaded.
		/// </summary>
		public static EventHandler<List<Reaction>> ReactionAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new reaction.
		/// </summary>
		public static EndpointEventHandler<ReactionAutoForm> ReactionCreate;
		/// <summary>
		/// Delete a reaction.
		/// </summary>
		public static EndpointEventHandler<Reaction> ReactionDelete;
		/// <summary>
		/// Update reaction metadata.
		/// </summary>
		public static EndpointEventHandler<ReactionAutoForm> ReactionUpdate;
		/// <summary>
		/// Load reaction metadata.
		/// </summary>
		public static EndpointEventHandler<Reaction> ReactionLoad;
		/// <summary>
		/// List reactions.
		/// </summary>
		public static EndpointEventHandler<Filter<Reaction>> ReactionList;

		#endregion

		#region Service events

		/// <summary>
		/// Just before a new ReactionType is created. The given ReactionType won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<ReactionType> ReactionTypeBeforeCreate;

		/// <summary>
		/// Just after a ReactionType has been created. The given ReactionType object will now have an ID.
		/// </summary>
		public static EventHandler<ReactionType> ReactionTypeAfterCreate;

		/// <summary>
		/// Just before a ReactionType is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<ReactionType> ReactionTypeBeforeDelete;

		/// <summary>
		/// Just after a ReactionType has been deleted.
		/// </summary>
		public static EventHandler<ReactionType> ReactionTypeAfterDelete;

		/// <summary>
		/// Just before updating a ReactionType. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<ReactionType> ReactionTypeBeforeUpdate;

		/// <summary>
		/// Just after updating a ReactionType.
		/// </summary>
		public static EventHandler<ReactionType> ReactionTypeAfterUpdate;

		/// <summary>
		/// Just after a ReactionType was loaded.
		/// </summary>
		public static EventHandler<ReactionType> ReactionTypeAfterLoad;

		/// <summary>
		/// Just before a service loads a ReactionType list.
		/// </summary>
		public static EventHandler<Filter<ReactionType>> ReactionTypeBeforeList;

		/// <summary>
		/// Just after a ReactionType list was loaded.
		/// </summary>
		public static EventHandler<List<ReactionType>> ReactionTypeAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new ReactionType.
		/// </summary>
		public static EndpointEventHandler<ReactionTypeAutoForm> ReactionTypeCreate;
		/// <summary>
		/// Delete a ReactionType.
		/// </summary>
		public static EndpointEventHandler<ReactionType> ReactionTypeDelete;
		/// <summary>
		/// Update ReactionType metadata.
		/// </summary>
		public static EndpointEventHandler<ReactionTypeAutoForm> ReactionTypeUpdate;
		/// <summary>
		/// Load ReactionType metadata.
		/// </summary>
		public static EndpointEventHandler<ReactionType> ReactionTypeLoad;
		/// <summary>
		/// List ReactionTypes.
		/// </summary>
		public static EndpointEventHandler<Filter<ReactionType>> ReactionTypeList;

		#endregion

	}

}
