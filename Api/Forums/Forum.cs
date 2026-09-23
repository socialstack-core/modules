using Api.Database;
using Api.Startup;
using Api.Users;
using System;

namespace Api.Forums
{

	/// <summary>
	/// A particular forum board. These contain lists of threads.
	/// </summary>
	[HasVirtualField("stats", typeof(ForumStat), "Id")]
	public partial class Forum : VersionedContent<uint>
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