using System;
using System.Collections.Generic;
using Api.Database;
using Api.Reactions;
using Api.Users;

namespace Api.Channels
{

	/// <summary>
	/// A question channel.
	/// </summary>
	public partial class Channel : RevisionRow
	{
		/// <summary>
		/// Friendly name of this channel.
		/// </summary>
		public string Name;
		
		/// <summary>
		/// The page that this channel is viewed on.
		/// </summary>
		public int PageId;

        /// <summary>
        /// The icon ref. See also: "Upload.Ref" in the Uploads module.
        /// </summary>
        [DatabaseField(Length = 80)]
        public string IconRef;

    }

}