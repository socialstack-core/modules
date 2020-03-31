using Api.Answers;
using Api.Questions;
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
		/// <summary>
		/// Set of events for a QuestionBoard.
		/// </summary>
		public static EventGroup<QuestionBoard> QuestionBoard;
		
		/// <summary>
		/// Set of events for a Answer.
		/// </summary>
		public static EventGroup<Answer> Answer;
		
		/// <summary>
		/// Set of events for a Question.
		/// </summary>
		public static EventGroup<Question> Question;
	}
}