using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Contacts
{
	
	/// <summary>
	/// A contact, typically used in e.g. help guides or knowledge bases.
	/// </summary>
	public partial class Contact : RevisionRow
	{
		/// <summary>
		/// The name of the person contacting
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Name;

		/// <summary>
		/// The email of the person contacting.
		/// </summary>
		public string Email;
		
		/// <summary>
		/// The content of this contact message.
		/// </summary>
		public string BodyJson;

        /// <summary>
        /// The Subject of the article
        /// </summary>
        [DatabaseField(Length = 500)]
        public string Subject;     
	}
	
}