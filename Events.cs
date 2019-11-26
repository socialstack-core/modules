using Api.QuestionBoards;
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
		/// Just before a new question board is created. The given question board won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<QuestionBoard> QuestionBoardBeforeCreate;

		/// <summary>
		/// Just after an question board has been created. The given question board object will now have an ID.
		/// </summary>
		public static EventHandler<QuestionBoard> QuestionBoardAfterCreate;

		/// <summary>
		/// Just before an question board is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<QuestionBoard> QuestionBoardBeforeDelete;

		/// <summary>
		/// Just after an question board has been deleted.
		/// </summary>
		public static EventHandler<QuestionBoard> QuestionBoardAfterDelete;

		/// <summary>
		/// Just before updating an question board. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<QuestionBoard> QuestionBoardBeforeUpdate;

		/// <summary>
		/// Just after updating an question board.
		/// </summary>
		public static EventHandler<QuestionBoard> QuestionBoardAfterUpdate;

		/// <summary>
		/// Just after an question board was loaded.
		/// </summary>
		public static EventHandler<QuestionBoard> QuestionBoardAfterLoad;

		/// <summary>
		/// Just before a service loads an questionBoard list.
		/// </summary>
		public static EventHandler<Filter<QuestionBoard>> QuestionBoardBeforeList;

		/// <summary>
		/// Just after an questionBoard list was loaded.
		/// </summary>
		public static EventHandler<List<QuestionBoard>> QuestionBoardAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new question board.
		/// </summary>
		public static EndpointEventHandler<QuestionBoardAutoForm> QuestionBoardCreate;
		/// <summary>
		/// Delete an question board.
		/// </summary>
		public static EndpointEventHandler<QuestionBoard> QuestionBoardDelete;
		/// <summary>
		/// Update question board metadata.
		/// </summary>
		public static EndpointEventHandler<QuestionBoardAutoForm> QuestionBoardUpdate;
		/// <summary>
		/// Load question board metadata.
		/// </summary>
		public static EndpointEventHandler<QuestionBoard> QuestionBoardLoad;
		/// <summary>
		/// List question boards.
		/// </summary>
		public static EndpointEventHandler<Filter<QuestionBoard>> QuestionBoardList;

		#endregion

	}

}
