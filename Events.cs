using Api.Quizzes;
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
		/// Set of events for a Quiz.
		/// </summary>
		public static EventGroup<Quiz> Quiz;
		
		/// <summary>
		/// Set of events for a QuizQuestion.
		/// </summary>
		public static EventGroup<QuizQuestion> QuizQuestion;
		
		/// <summary>
		/// Set of events for a QuizAnswer.
		/// </summary>
		public static EventGroup<QuizAnswer> QuizAnswer;
	}
}