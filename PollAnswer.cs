using System;
using Api.Database;
using Api.Translate;
using Api.Users;
using Api.Startup;


namespace Api.Polls
{
	
	/// <summary>
	/// A Poll
	/// </summary>
	[ListAs("PollAnswers")]
	[HasVirtualField("Poll", typeof(Poll), "PollId")]
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