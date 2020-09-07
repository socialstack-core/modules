using System;
using Api.Database;
using Api.Translate;
using Api.Users;
using Api.WebSockets;


namespace Api.PubQuizzes
{
	
	/// <summary>
	/// A PubQuizSubmission
	/// </summary>
	public partial class PubQuizSubmission : RevisionRow, IAmLive
	{
		// Example fields. None are required:
		/// <summary>
		/// The activity instance id this submission is for.
		/// </summary>
		public int ActivityInstanceId;

		/// <summary>
		/// The id of the pub quiz answer
		/// </summary>
		public int PubQuizAnswerId;	

		/// <summary>
		/// The PubQuiz answer that was submitted.
		/// </summary>
		public object PubQuizAnswer { get; set; }

		/// <summary>
		/// Determins if the answer chosen was correct. 
		/// </summary>
		public bool IsCorrect;
	}
}