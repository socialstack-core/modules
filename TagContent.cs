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
		public uint TagId;

		/// <summary>
		/// ID of the tag.
		/// </summary>
		public override uint TargetContentId
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