using System;
using Api.Database;
using Api.Translate;


namespace Api.Categories
{
	
	/// <summary>
	/// Content within a particular category.
	/// </summary>
	public partial class CategoryContent : MappingRow
	{
		/// <summary>
		/// The ID of the category.
		/// </summary>
		public int CategoryId;
		
		/// <summary>
		/// ID of the category.
		/// </summary>
		public override int TargetContentId
		{
			get{
				return CategoryId;
			}
			set {
				CategoryId = value;
			}
		}
	}
	
}