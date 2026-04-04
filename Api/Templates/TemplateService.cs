using Api.CanvasRenderer;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.NavMenus;
using Api.Pages;
using Api.Permissions;
using Api.Startup;
using Api.Startup.Routing;
using Api.Templates;
using Api.Translate;
using Api.Uploader;
using Api.Users;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Templates
{
	/// <summary>
	/// Handles templates.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class TemplateService : AutoService<Template>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public TemplateService(AdminNavMenuItemService _) : base(Events.Template)
        {

			InitEvents();

			InstallAdminPages("Templates", "fa:fa-file-medical", ["id", "title", "key"], null, "content_management");
			Cache();

			// Install the two default templates (referenced by AddTemplate inside PageBuilder):
			InstallContent(new TemplateBuilder()
			{
				Title = "Site default",
				Key = "site_default",
				BuildBody = (TemplateBuilder builder) => {
					return new CanvasNode("UI/Templates/BaseWebTemplate")
						.AddRoot("body",
							new CanvasNode("Admin/Template/Slot")
							.With("name", "body")
						);
				}
			});

			InstallContent(new TemplateBuilder()
			{
				Title = "Admin default",
				Key = "admin_default",
				BuildBody = (TemplateBuilder builder) => {
					return new CanvasNode("Admin/Templates/BaseAdminTemplate")
						.AddRoot("children", 
							new CanvasNode("Admin/Template/Slot")
							.With("name", "body")
						);
				}
			});

			Events.Page.TransformCanvasNode.AddEventListener(async (Context context, CanvasNode node) => {

				if (node == null || node.Source == null)
				{
					return node;
				}

				if (node.Module == "Admin/Template")
				{
					if (node.Data == null)
					{
						return null;
					}

					// Sub in the template
					Template template = null;

					if(node.Data.TryGetValue("id", out CanvasNode.CanvasAttribute templateIdObj))
					{
						var templateId = templateIdObj.Value as long?;

						if (templateId.HasValue)
						{
							// Load the template:
							template = await Get(context, (uint)(templateId.Value), DataOptions.IgnorePermissions);

							if (template == null)
							{
								// Can't load this node as the template was deleted.
								Log.Warn(LogTag, $"A template was missing Admin/Template :: {templateId.Value} ");
								return null;
							}
						}
					}
					else if (node.Data.TryGetValue("templateKey", out CanvasNode.CanvasAttribute templateKeyObj))
					{
						var templateKey = templateKeyObj.Value as string;
						template = await Where("Key=?").Bind(templateKey).First(context);

						if (template == null)
						{
							// Can't load this node as the template was deleted.
							Log.Warn(LogTag, $"A template was missing Admin/Template :: {templateKey} ");
							return null;
						}
					}

					if (template == null)
					{
						// Can't load this node as the template was deleted.
						Log.Warn(LogTag, $"Unable to locate admin template.");
						return null;
					}

					// Load the template body (which can cause further substition if needed):
					var templateInfo = await LoadTemplate(context, template, node, node.Canvas);

					return templateInfo.LoadedTemplate;
				}
				else if (node.Module == "Admin/Template/Slot")
				{
					// Replace the slot with the provided slot data from the template.
					if (node.Canvas == null || node.Canvas.Template == null)
					{
						Log.Warn(LogTag, "A template slot node is present on a non-template canvas.");
						return null;
					}

					if (node.Data == null)
					{
						Log.Warn(LogTag, "A template slot is missing its data so it cannot be populated.");
						return null;
					}

					// The template info tells us 
					var templateInfo = node.Canvas.Template;

					// Locate the root replacement for this slot:
					if (templateInfo.Config.Roots == null || !node.Data.TryGetValue("name", out CanvasNode.CanvasAttribute rootName))
					{
						Log.Warn(LogTag, "Ignoring either a template slot which has no name, or no source config for the slot");
						return null;
					}

					if (rootName == null || !templateInfo.Config.Roots.TryGetValue((string)rootName.Value, out CanvasNode root))
					{
						// This one is intentionally silent.
						// It'll happen if a slot was optional and the user simply didn't populate it.
						return null;
					}
					
					// Substitution time:
					return root;
				}

				return node;
			});

			Events.Template.AfterUpdate.AddEventListener((Context context, Template template, ChangedFields diff) =>
			{
				// Need to update the two caches. We'll just wipe them for now:
				Router.RequestRebuild();

				return new ValueTask<Template>(template);
			});

			Events.Template.AfterCreate.AddEventListener((Context context, Template template) =>
			{
				// Need to update the two caches. We'll just wipe them for now:
				Router.RequestRebuild();

				return new ValueTask<Template>(template);
			});

			Events.Template.BeforeCreate.AddEventListener((Context context, Template template) => {

				if (string.IsNullOrEmpty(template.Title))
				{
					throw new PublicException("Title is required", "template/title_required");
				}

				if (string.IsNullOrEmpty(template.Key))
				{
					template.Key = template.Title.ToLower().Trim().Replace(" ", "_");
				}

				return new ValueTask<Template>(template);

			});

		}

		/// <summary>
		/// Populates a Template from a TemplateBuilder.
		/// </summary>
		protected override async ValueTask PopulateContent(Context context, Template content, ContentBuilder builder)
		{
			if (builder is TemplateBuilder templateBuilder)
			{
				content.Title = templateBuilder.Title;
				content.TemplateType = (uint)templateBuilder.TemplateType;
				var json = await templateBuilder.Build(context);
				content.BodyJson = new JsonString(json.ToJson());
			}
		}

		private void InitEvents()
        {

			Events.Page.BeforePageInstall.AddEventListener((Context context, PageBuilder builder) =>
			{
				if (builder == null || builder.ContentType != typeof(Template))
				{
					return ValueTask.FromResult(builder);
				}

				if (builder.PageType == CommonPageType.AdminEdit)
				{
					builder.GetContentRoot()
						.Empty()
						.AppendChild(
							new CanvasNode("Admin/Template/AddEdit")
							.WithPrimaryLink("content")
						);
				}
				else if (builder.PageType == CommonPageType.AdminAdd)
				{
					builder.GetContentRoot()
						.Empty()
						.AppendChild(new CanvasNode("Admin/Template/AddEdit"));
				}

				return new ValueTask<PageBuilder>(builder);
			}, 2);
			
			
        }

		// protected void InstallAdminPages(string navMenuLabel, string navMenuIconRef, string[] fields, ChildAdminPageOptions childAdminPage = null, string visibilityJson = null)
		// {

		// }

		/// <summary>
		/// Loads a template.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="template"></param>
		/// <param name="templateConfig"></param>
		/// <param name="canvDetails"></param>
		/// <returns></returns>
		public async ValueTask<TemplateDetails> LoadTemplate(Context context, Template template, CanvasNode templateConfig, CanvasDetails canvDetails)
		{
			// Load the JSON.
			var json = Newtonsoft.Json.JsonConvert.DeserializeObject(template.BodyJson.ValueOf()) as JToken;

			var details = new TemplateDetails()
			{
				Template = template,
				Config = templateConfig
			};

			var prevTemplate = canvDetails.Template;
			canvDetails.Template = details;
			var templateBody = await CanvasNode.LoadCanvasNode(context, json, canvDetails);
			canvDetails.Template = prevTemplate;
			details.LoadedTemplate = templateBody;
			return details;
		}
		
	}

	/// <summary>
	/// Details for a template.
	/// </summary>
	public class TemplateDetails
	{
		/// <summary>
		/// The template node.
		/// </summary>
		public CanvasNode LoadedTemplate;
		
		/// <summary>
		/// The node providing the instance config.
		/// This is where the roots are that will be placed into slots.
		/// </summary>
		public CanvasNode Config;

		/// <summary>
		/// The template itself.
		/// </summary>
		public Template Template;

	}


}

namespace Api.CanvasRenderer {
	public partial class CanvasDetails
	{
		/// <summary>
		/// The template it is a part of.
		/// </summary>
		public TemplateDetails Template;
	}
}