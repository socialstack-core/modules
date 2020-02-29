using Newtonsoft.Json;
using Api.AutoForms;


namespace Api.Templates
{
    /// <summary>
    /// Used when creating or updating a template
    /// </summary>
    public partial class TemplateAutoForm : AutoForm<Template>
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
		/// Template canvas JSON. The raw structure of the template.
		/// </summary>
		public string BodyJson;
	}
}
