using Newtonsoft.Json;
using Api.AutoForms;
using System;

namespace Api.StaffMembers
{
    /// <summary>
    /// Used when creating or updating a staff member
    /// </summary>
    public partial class PersonAutoForm : AutoForm<StaffMember>
    {
		/// <summary>
		/// The first name of the StaffMember (and any middle names).
		/// </summary>
		public string FirstName;

		/// <summary>
		/// True if they're featured.
		/// </summary>
		public bool IsFeatured;
		
		/// <summary>
		/// The last name of the StaffMember.
		/// </summary>
		public string LastName;

		/// <summary>
		/// A short description of the StaffMember.
		/// </summary>
		public string Description;

		/// <summary>
		/// The Staff Member's job title.
		/// </summary>
		public string JobTitle;

		/// <summary>
		/// The ref for a photo of this staff member. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		public string PhotoRef;
	}
}
