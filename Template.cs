using System;
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
		/// The content (as canvas JSON).
		/// </summary>
		public string BodyJson;
	}
	
}