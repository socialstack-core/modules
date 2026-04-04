using Api.Pages;
using Api.Permissions;
using Api.Templates;
using System.Collections.Generic;

namespace Api.Eventing
{
	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
		/// <summary>
		/// All template entity events.
		/// </summary>
		public static TemplateEventGroup Template;
	}

	/// <summary>
	/// Events for a template.
	/// </summary>
	public class TemplateEventGroup : EventGroup<Template> {

		/// <summary>
		/// On template install.
		/// </summary>
		public EventHandler<TemplateBuilder> BeforeTemplateInstall;

	}
}