using System;
using Api.AutoForms;
using Api.Database;
using Api.Permissions;
using Api.Translate;
using Api.Users;

namespace Api.Pages
{
	
	/// <summary>
	/// A page.
	/// </summary>
	public partial class Page : VersionedContent<uint>
	{
		/// <summary>
		/// The URL for this page.
		/// </summary>
		public string Url;
		
		/// <summary>
		/// The default title for this page.
		/// </summary>
		[Localized]
		public string Title;
		
		/// <summary>
		/// The pages content (as canvas JSON).
		/// </summary>
		[Localized]
		[Data("groups", "*")]
		// default max length is 64Kb - uncomment following line to expand this (9Mb in the following example):
		//[DatabaseField(Length = 9000000)]
		public string BodyJson;

		/// <summary>
		/// The default description for this page.
		/// </summary>
		[Localized]
		public string Description;
		
		/// <summary>
		/// A disambiguation mechanism when the permission system returns multiple pages.
		/// Typically happens on the homepage.
		/// </summary>
		public bool PreferIfLoggedIn;
	}
	
}