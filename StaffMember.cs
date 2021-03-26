using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.StaffMembers
{
	
	/// <summary>
	/// A Staff Member that isn't a user - used for e.g. staff lists.
	/// </summary>
	public partial class StaffMember : VersionedContent<int>
	{
		/// <summary>
		/// The first name of the Staff Member (and any middle names).
		/// </summary>
		[DatabaseField(Length = 50)]
		public string FirstName;

		/// <summary>
		/// The last name of the Staff Member.
		/// </summary>
		[DatabaseField(Length = 50)]
		public string LastName;
		
		/// <summary>
		/// True if they're featured.
		/// </summary>
		public bool IsFeatured;
		
		/// <summary>
		/// The Staff Member's job title.
		/// </summary>
		[DatabaseField(Length = 50)]
		[Localized]
		public string JobTitle;

		/// <summary>
		/// A short description of the Staff Member.
		/// </summary>
		[DatabaseField(Length = 200)]
		[Localized]
		public string Description;
		
		/// <summary>
		/// The ref for a photo of this Staff Member. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string PhotoRef;
		
	}

}