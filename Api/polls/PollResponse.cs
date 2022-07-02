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
	public partial class PollResponse : Content<uint>
	{
		/// <summary>
		/// The time this response was created.
		/// </summary>
		public DateTime CreatedUtc;
		
		/// <summary>
		/// The user Id.
		/// </summary>
		public uint UserId;
		
		/// <summary>
		/// The selected answer.
		/// </summary>
		public uint AnswerId;
		
		/// <summary>
		/// The parent poll ID
		/// </summary>
		public uint PollId;
	}

}