using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Polls
{
	
	/// <summary>
	/// A Poll
	/// </summary>
	public partial class Poll : RevisionRow
	{
        /// <summary>
        /// The name of the poll
        /// </summary>
        [DatabaseField(Length = 200)]
		[Localized]
		public string Title;

		/// <summary>
		/// A page this poll appears on.
		/// </summary>
		public int PageId;
	}

}