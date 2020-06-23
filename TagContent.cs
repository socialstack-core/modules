using System;
using Api.Database;
using Api.Translate;


namespace Api.Tags
{
	
	/// <summary>
	/// Content tagged with a particular tag.
	/// </summary>
	public partial class TagContent : DatabaseRow
	{
		/// <summary>
		/// The ID of the tag.
		/// </summary>
		public int TagId;
		/// <summary>
		/// The type ID of the tagged content. See also: Api.Database.ContentTypes
		/// </summary>
		public int ContentTypeId;
		/// <summary>
		/// The ID of the tagged content.
		/// </summary>
		public int ContentId;
		/// <summary>
		/// The UTC creation date. Read/ delete only rows so an edited date isn't present here.
		/// </summary>
		public DateTime CreatedUtc;

		/// <summary>
		/// The ID of a particular revision that these tags are on. Zero if it's on the live content.
		/// </summary>
		public int RevisionId;
	}
	
}