using System;
using Api.Database;
using Api.Translate;


namespace Api.Categories
{
	
	/// <summary>
	/// Content within a particular category.
	/// </summary>
	public partial class CategoryContent : MappingEntity
	{
		/// <summary>
		/// The ID of the category.
		/// </summary>
		public uint CategoryId;
		
		/// <summary>
		/// ID of the category.
		/// </summary>
		public override uint TargetContentId
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