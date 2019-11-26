namespace Api.Permissions
{

	/// <summary>
	/// Capabilities are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Capabilities
    {

		/// <summary>
		/// Create an email template.
		/// </summary>
		public static Capability EmailTemplateCreate;

		/// <summary>
		/// Update an email template.
		/// </summary>
		public static Capability EmailTemplateUpdate;

		/// <summary>
		/// Delete an email template.
		/// </summary>
		public static Capability EmailTemplateDelete;

		/// <summary>
		/// Search email templates.
		/// </summary>
		public static Capability EmailTemplateSearch;

		/// <summary>
		/// Send a test of an email template.
		/// </summary>
		public static Capability EmailTemplateSendTest;

		/// <summary>
		/// Load an email template.
		/// </summary>
		public static Capability EmailTemplateLoad;

	}

}
