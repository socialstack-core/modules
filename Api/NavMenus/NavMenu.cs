using Api.AutoForms;
using Api.Database;
using Api.Translate;
using Api.Users;
using System;
using System.Collections.Generic;

namespace Api.NavMenus
{
	
	/// <summary>
	/// A particular nav menu.
	/// </summary>
	public partial class NavMenu : InstallableContent<uint>
	{
		/// <summary>
		/// The name of the menu in the site default language.
		/// </summary>
		[Data("required", true)]
		public string Name;

		/// <summary>
		/// The JSON content of this menu.
		/// </summary>
		[Module("Admin/NavMenu/Editor")]
		public Localized<JsonString> ContentJson;
	}

}