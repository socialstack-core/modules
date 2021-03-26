using System;
using Api.Database;
using Api.Translate;


namespace Api.Tags
{

	/// <summary>
	/// Content tagged with a particular tag.
	/// </summary>
	public partial class TagContent : MappingEntity
	{
		/// <summary>
		/// The ID of the tag.
		/// </summary>
		public int TagId;

		/// <summary>
		/// ID of the tag.
		/// </summary>
		public override int TargetContentId
		{
			get{
				return TagId;
			}
			set {
				TagId = value;
			}
		}
	}
}