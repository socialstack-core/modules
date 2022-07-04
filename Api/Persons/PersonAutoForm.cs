using Newtonsoft.Json;
using Api.AutoForms;
using System;

namespace Api.Persons
{
    /// <summary>
    /// Used when creating or updating a person
    /// </summary>
    public partial class PersonAutoForm : AutoForm<Person>
    {
		/// <summary>
		/// The first name of the person (and any middle names).
		/// </summary>
		public string FirstName;

		/// <summary>
		/// The last name of the person.
		/// </summary>
		public string LastName;

		/// <summary>
		/// A short description of the person.
		/// </summary>
		public string Description;

		/// <summary>
		/// The person's job title.
		/// </summary>
		public string JobTitle;

		/// <summary>
		/// The ref for a photo of this person. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		public string PhotoRef;
	}
}
