using System;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;


namespace Api.MixedContents
{

	/// <summary>
	/// A MixedContent
	/// </summary>
	[CacheOnly]
	[HasVirtualField("content", "ContentType", "Id")]
	public partial class MixedContent : Content<ulong>
	{
		/// <summary>
		/// The content type to collect.
		/// </summary>
		public string ContentType;
	}

}