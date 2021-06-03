using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Polls
{
	
	/// <summary>
	/// A Poll
	/// </summary>
	public partial class PollAnswer : VersionedContent<uint>
	{
        /// <summary>
        /// Title text
        /// </summary>
        [DatabaseField(Length = 200)]
		[Localized]
		public string Title;

		/// <summary>
		/// The parent poll ID
		/// </summary>
		public uint PollId;
		
		/// <summary>
		/// Total votes for this answer.
		/// </summary>
		public int Votes;
	}

}