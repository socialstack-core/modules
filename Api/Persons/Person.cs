using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Persons
{
	
	/// <summary>
	/// A person that isn't a user - used for e.g. staff lists.
	/// </summary>
	public partial class Person : RevisionRow
	{
		/// <summary>
		/// The first name of the person (and any middle names).
		/// </summary>
		[DatabaseField(Length = 50)]
		public string FirstName;

		/// <summary>
		/// The last name of the person.
		/// </summary>
		[DatabaseField(Length = 50)]
		public string LastName;

		/// <summary>
		/// The person's job title.
		/// </summary>
		[DatabaseField(Length = 50)]
		[Localized]
		public string JobTitle;

		/// <summary>
		/// A short description of the person.
		/// </summary>
		[DatabaseField(Length = 200)]
		[Localized]
		public string Description;
		
		/// <summary>
		/// The ref for a photo of this person. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string PhotoRef;
		
	}

}