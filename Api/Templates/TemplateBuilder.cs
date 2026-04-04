using Api.CanvasRenderer;
using Api.Contexts;
using Api.Eventing;
using Api.Users;
using Microsoft.AspNetCore.Routing.Template;
using System;
using System.Threading.Tasks;

namespace Api.Templates
{
	/// <summary>
	/// Template type.
	/// </summary>
	public enum TemplateType : int {
		/// <summary>
		/// Web template (pages and page templates)
		/// </summary>
		Web = 1,
		/// <summary>
		/// Email template (emails and email templates)
		/// </summary>
		Email = 2
		// PDF unused.
	}

	/// <summary>
	/// Used to auto install templates
	/// </summary>
	public class TemplateBuilder : ContentBuilder
	{
		/// <summary>
		/// The title of the template.
		/// </summary>
		public string Title;

		/// <summary>
		/// The template type.
		/// </summary>
		public TemplateType TemplateType = TemplateType.Web;

		/// <summary>
		/// The generator for the template body.
		/// </summary>
		public Func<TemplateBuilder, CanvasNode> BuildBody;

		private CanvasNode _body;

		/// <summary>
		/// The built body (if there is one yet) 
		/// </summary>
		public CanvasNode Body
		{
			get
			{
				return _body;
			}
		}

		/// <summary>
		/// Builds the body of this template, triggering the BeforeTemplateInstall event for things to interject if they wish.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public async ValueTask<CanvasNode> Build(Context context)
		{
			_body = BuildBody(this);
			await Events.Template.BeforeTemplateInstall.Dispatch(context, this);
			return Body;
		}

	}
}
