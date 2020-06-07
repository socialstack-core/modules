using System;
using Api.Database;
using Api.Users;

namespace Api.Forums
{
	
	/// <summary>
	/// A forum thread. These contain a list of replies.
	/// </summary>
	public partial class ForumThread : RevisionRow
	{
		/// <summary>
		/// The forum this thread is in.
		/// </summary>
		public int ForumId;
		/// <summary>
		/// The primary ID of the page that this thread appears on.
		/// </summary>
		public int PageId;
		/// <summary>
		/// The thread title in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Title;
		/// <summary>
		/// The JSON body of the main post. It's JSON because it is a *canvas*. 
		/// This means the reply can easily include other components such as polls etc 
		/// and be formatted in complex ways.
		/// </summary>
		// [DatabaseField(Length = 8000)]
		public string BodyJson;
		
		/// <summary>
		/// Total replies.
		/// </summary>
		public int ReplyCount;
	}
	
}