using Api.AutoForms;
using System;


namespace Api.Database
{
	
	/// <summary>
	/// Maps e.g. tags to particular content.
	/// </summary>
	public abstract class MappingEntity : Entity<int>
	{
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

		/// <summary>
		/// Target content ID (e.g. ID of the target tag).
		/// </summary>
		public virtual int TargetContentId
		{
			get
			{
				return 0;
			}
			set
			{			
			}
		}
	}
}