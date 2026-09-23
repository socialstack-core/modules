using Api.Database;
using Api.Startup;
using Api.Users;
using System;

namespace Api.Forums
{

	/// <summary>
	/// A forum thread. These contain a list of replies.
	/// </summary>
	[HasVirtualField("stats", typeof(ForumThreadStat), "Id")]
	public partial class ForumThread : VersionedContent<uint>
	{
		/// <summary>
		/// The forum this thread is in.
		/// </summary>
		public uint ForumId;
		/// <summary>
		/// The thread title in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Title;

		/// <summary>
		/// The text body of the main post, optionally in markdown.
		/// Users can attach other objects to the thread/ post, such as polls, images, etc.
		/// </summary>
		public string BodyText;
	}
	
}