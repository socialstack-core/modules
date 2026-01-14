using System;
using Api.AutoForms;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.PublishGroups
{

	/// <summary>
	/// A PublishGroup. Created and manipulated by users but doesn't have revisions itself.
	/// </summary>
	public partial class PublishGroup : UserCreatedContent<uint>
	{
        /// <summary>
        /// The internal name of the group, used internally by admins only
        /// </summary>
        [DatabaseField(Length = 200)]
        [Data("required", true)]
        [Data("validate", "Required")]
		public string Name;

		/// <summary>
		/// True if this group has been published.
		/// </summary>
		public bool IsPublished;

		/// <summary>
		/// Group is ready for publishing and if it has an automatic date, will go live at that time.
		/// </summary>
		public bool ReadyForPublishing;

		/// <summary>
		/// A time to auto publish this group.
		/// </summary>
		public DateTime? AutoPublishTimeUtc;

	}

}