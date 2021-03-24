using System;
using Api.AutoForms;
using Api.Database;
using Api.Users;

namespace Api.Templates
{
	
	/// <summary>
	/// A template.
	/// </summary>
	public partial class Template : RevisionRow
	{
		/// <summary>
		/// A key used to identify a template by its purpose.
		/// E.g. "default" or "admin_default"
		/// </summary>
		public string Key;

		/// <summary>
		/// The default title for this template.
		/// </summary>
		public string Title;
		
		/// <summary>
		/// The module groups that this is available for. "formatting" is the default. * is "all of them".
		/// </summary>
		public string ModuleGroups = "formatting";
		
		/// <summary>
		/// The content (as canvas JSON).
		/// </summary>
		[Data("groups", "*")]
		[Data("withIds", "1")]
		public string BodyJson;
	}
	
}