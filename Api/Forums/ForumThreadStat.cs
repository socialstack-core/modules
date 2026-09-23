using System;
using Api.Database;
using Api.Startup;
using Api.Users;

namespace Api.Forums
{

	/// <summary>
	/// A forum thread statistical data object. Separate from the thread to prevent lots of revisions being created when the thread is replied to.
	/// </summary>
	[HasVirtualField("thread", typeof(ForumThread), "Id")]
	public partial class ForumThreadStat : UserCreatedContent<uint>
	{
		/// <summary>
		/// The number of replies on a forum thread.
		/// </summary>
		public uint ReplyCount;

		/// <summary>
		/// Most recent reply date in UTC. Can be used to sort threads by most recent activity.
		/// </summary>
		public DateTime LatestReplyUtc;
	}
	
}