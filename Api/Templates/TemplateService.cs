using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Api.CanvasRenderer;
using Api.NavMenus;
using Newtonsoft.Json.Linq;
using Api.Templates;
using Api.Startup;
using Api.Pages;
using Api.Uploader;

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
			Install(new Template()
			{
				Title = "Site default",
				Key = "site_default",
				BodyJson = new JsonString(@"{
					""t"": ""UI/Templates/BaseWebTemplate"",
					""r"": {
						""body"": {
							""t"": ""Admin/Template/Slot"",
							""d"": {""name"": ""body""}
						}
					}
				}")
			});

			Install(new Template()
			{
				Title = "Admin default",
				Key = "admin_default",
				BodyJson = new JsonString(@"{
					""t"": ""Admin/Templates/BaseAdminTemplate"",
					""r"": {
						""children"": {
							""t"": ""Admin/Template/Slot"",
							""d"": {""name"": ""body""}
						}
					}
				}")
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
						}
					}
					else if (node.Data.TryGetValue("templateKey", out CanvasNode.CanvasAttribute templateKeyObj))
					{
						var templateKey = templateKeyObj.Value as string;
						template = await Where("Key=?").Bind(templateKey).First(context);
					}

					if (template == null)
					{
						// Can't load this node as the template was deleted.
						return null;
					}

					// Load the template body (which can cause further substition if needed):
					var templateInfo = await LoadTemplate(context, template, node);

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

		/// <summary>
		/// The built up list of templates to install when services have started.
		/// </summary>
		private List<Template> _toInstall;
		private object _installLocker = new object();

		/// <summary>
		/// Installs the given templates. It checks if they exist by their key, and if not, creates them.
		/// </summary>
		/// <param name="builders">
		/// </param>
		public void Install(params Template[] builders)
		{
			bool scheduleStart = false;

			lock (_installLocker)
			{
				if (_toInstall == null)
				{
					_toInstall = new List<Template>();
					scheduleStart = true;
				}

				_toInstall.AddRange(builders);
			}

			if (scheduleStart)
			{
				if (Services.Started)
				{
					Task.Run(async () =>
					{
						List<Template> set;

						lock (_installLocker)
						{
							set = _toInstall;
							_toInstall = null;
						}
						await InstallInternal(new Context(), set);
					});
				}
				else
				{
					Events.Service.AfterStart.AddEventListener(async (Context ctx, object src) =>
					{
						List<Template> set;

						lock (_installLocker)
						{
							set = _toInstall;
							_toInstall = null;
						}
						await InstallInternal(ctx, set);
						return src;
					}, 5);
				}
			}
		}

		private async ValueTask InstallInternal(Context context, List<Template> set)
		{
			if (set == null)
			{
				return;
			}

			foreach (var template in set)
			{
				if (string.IsNullOrEmpty(template.Key))
				{
					throw new ArgumentException("A Key is required when installing a template.");
				}
			}

			// Get the templates by those keys:
			var existingTemplates = (await Where("Key=[?]", DataOptions.NoCacheIgnorePermissions)
					.Bind(set.Select(temp => temp.Key))
					.ListAll(context));

			var existingTemplateLookup = new Dictionary<string, Template>();

			foreach (var pg in existingTemplates)
			{
				existingTemplateLookup[pg.Key] = pg;
			}

			foreach (var template in set)
			{
				// If it doesn't already exist, create it.
				if (existingTemplateLookup.ContainsKey(template.Key))
				{
					continue;
				}

				await Create(context, template, DataOptions.IgnorePermissions);
			}
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
		/// <returns></returns>
		public async ValueTask<TemplateDetails> LoadTemplate(Context context, Template template, CanvasNode templateConfig)
		{
			// Load the JSON.
			var json = Newtonsoft.Json.JsonConvert.DeserializeObject(template.BodyJson.ValueOf()) as JToken;

			var details = new TemplateDetails()
			{
				Template = template,
				Config = templateConfig
			};

			var templateBody = await CanvasNode.LoadCanvasNode(context, json, new CanvasDetails()
			{
				Template = details
			});

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