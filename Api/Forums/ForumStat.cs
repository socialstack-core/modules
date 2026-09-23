using Api.Database;
using Api.Startup;
using Api.Users;
using System;

namespace Api.Forums
{

	/// <summary>
	/// A forum statistical data object. Separate from the forum to prevent lots of revisions being created when threads are replied to.
	/// </summary>
	[HasVirtualField("forum", typeof(Forum), "Id")]
	public partial class ForumStat : UserCreatedContent<uint>
	{
		/// <summary>
		/// The number of threads in a forum.
		/// </summary>
		public uint ThreadCount;

		/// <summary>
		/// The number of replies in a forum.
		/// </summary>
		public uint ReplyCount;

		/// <summary>
		/// Most recent reply date in UTC. Can be used to sort threads by most recent activity.
		/// </summary>
		public DateTime? LatestReplyUtc;
	}
	
}