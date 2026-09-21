using Api.AutoForms;
using Api.Database;
using Api.Permissions;
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
		/// Auto generated from a content type and filter.
		/// </summary>
		[Permissions(ReadRule = "false", Roles = "!admins")]
		[Permissions(ReadRule = "true", Roles = "admins")]
		[Module("Admin/NavMenu/GenConfigEditor")]
		public JsonString GeneratedMenuConfigJson;

		/// <summary>
		/// The JSON content of this menu.
		/// </summary>
		[Module("Admin/NavMenu/Editor")]
		public Localized<JsonString> ContentJson;
	}

}