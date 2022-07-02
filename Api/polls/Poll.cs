using System;
using Api.Database;
using Api.Translate;
using Api.Users;
using System.Collections.Generic;


namespace Api.Polls
{
	
	/// <summary>
	/// A Poll
	/// </summary>
	public partial class Poll : VersionedContent<uint>
	{
        /// <summary>
        /// The name of the poll
        /// </summary>
        [DatabaseField(Length = 200)]
		[Localized]
		public string Title;
	}

}