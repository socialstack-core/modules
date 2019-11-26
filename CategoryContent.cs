using System;
using Api.Database;
using Api.Translate;


namespace Api.Categories
{
	
	/// <summary>
	/// Content within a particular category.
	/// </summary>
	public partial class CategoryContent : DatabaseRow
	{
		/// <summary>
		/// The ID of the category.
		/// </summary>
		public int CategoryId;
		/// <summary>
		/// The type ID of the content in this category. See also: Api.Database.ContentTypes
		/// </summary>
		public int ContentTypeId;
		/// <summary>
		/// The ID of the content.
		/// </summary>
		public int ContentId;
		/// <summary>
		/// The UTC creation date. Read/ delete only rows so an edited date isn't present here.
		/// </summary>
		public DateTime CreatedUtc;
	}
	
}