using System;
using Api.AutoForms;
using Api.Database;
using Api.Users;

namespace Api.Templates
{
	
	/// <summary>
	/// A template.
	/// </summary>
	public partial class Template : VersionedContent<uint>
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
		/// The template description
		/// </summary>
		public string Description;

		/// <summary>
		/// The template type, defaults to web
		/// 1 = web
		/// 2 = email
		/// 3 = pdf (add as necessary)
		/// </summary>
		[Module("Admin/Template/TemplateTypeSelector")]
		public uint TemplateType = 1;
		
		/// <summary>
		/// The module groups that this is available for. "formatting" is the default. * is "all of them".
		/// </summary>
		public string ModuleGroups = "formatting";
		
		/// <summary>
		/// The content (as canvas JSON).
		/// </summary>
		[Data("groups", "*")]
		[Data("withIds", "1")]
		public JsonString BodyJson;
	}
	
}