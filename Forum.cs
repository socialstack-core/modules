using System;
using Api.Database;
using Api.Users;

namespace Api.Forums
{
	
	/// <summary>
	/// A particular forum board. These contain lists of threads.
	/// </summary>
	public partial class Forum : RevisionRow
	{
		/// <summary>
		/// The primary ID of the page that this forum appears on.
		/// </summary>
		public int PageId;

		/// <summary>
		/// The page ID that threads will appear on.
		/// </summary>
		public int ThreadPageId;

		/// <summary>
		/// The name of the forum in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Name;

		/// <summary>
		/// A short description of this forum.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Description;
		
		/// <summary>
		/// Cached thread count. Updated via an event.
		/// </summary>
		public int ThreadCount;

		/// <summary>
		/// Cached reply count. Updated via an event.
		/// </summary>
		public int ReplyCount;

		/// <summary>
		/// The UTC time of the latest reply.
		/// </summary>
		public DateTime? LatestReplyUtc;

		/// <summary>
		/// The feature image ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string FeatureRef;

		/// <summary>
		/// The icon ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string IconRef;
	}
	
}