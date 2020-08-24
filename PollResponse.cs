using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Polls
{
	
	/// <summary>
	/// A Poll
	/// </summary>
	[DatabaseIndex(Fields = new string[] { "UserId", "PollId" }, Unique = true)]
	public partial class PollResponse : DatabaseRow
	{
		/// <summary>
		/// The time this response was created.
		/// </summary>
		public DateTime CreatedUtc;
		
		/// <summary>
		/// The user Id.
		/// </summary>
		public int UserId;
		
		/// <summary>
		/// The selected answer.
		/// </summary>
		public int AnswerId;
		
		/// <summary>
		/// The parent poll ID
		/// </summary>
		public int PollId;
	}

}