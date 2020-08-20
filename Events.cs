using Api.PubQuizzes;
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
		/// Set of events for a PubQuiz.
		/// </summary>
		public static EventGroup<PubQuiz> PubQuiz;
		
		/// <summary>
		/// Set of events for a pubQuizQuestion.
		/// </summary>
		public static EventGroup<PubQuizQuestion> PubQuizQuestion;
		
		/// <summary>
		/// Set of events for a pubQuizAnswer.
		/// </summary>
		public static EventGroup<PubQuizAnswer> PubQuizAnswer;
	}
}