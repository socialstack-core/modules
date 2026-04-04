using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
using Api.Templates;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Components
{
	/// <summary>
	/// Handles componentGroups.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ComponentGroupService : AutoService<ComponentGroup>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ComponentGroupService() : base(Events.ComponentGroup)
        {
			InstallAdminPages("Component Groups", "fa:fa-folder", ["id", "name"], null, "content_management");

			InstallContent(
				new GroupBuilder
				{
					Key = "common_authoring",
					Name = "Common Authoring",
					AllowedComponents = new[] { "UI/Image", "UI/Video", "UI/Spacer" }
				},
				new GroupBuilder
				{
					Key = "admin_authoring",
					Name = "Admin Authoring",
					AllowedComponents = new[] { "common_authoring", "UI/Alert", "UI/Button", "UI/Collapsible", "UI/Dialog" }
				},
				new GroupBuilder
				{
					Key = "email_authoring",
					Name = "Email Authoring",
					AllowedComponents = new[] { "Email/*" }
				}
			);

			Events.ComponentGroup.BeforeCreate.AddEventListener((Context context, ComponentGroup group) => {

				if (string.IsNullOrEmpty(group.Name.GetFallback()))
				{
					throw new PublicException("Name is required", "componentgroup/name_required");
				}

				if (string.IsNullOrEmpty(group.Key))
				{
					group.Key = group.Name.GetFallback().ToLower().Trim().Replace(" ", "_");
				}

				return new ValueTask<ComponentGroup>(group);

			});

		}

		/// <summary>
		/// Populates a ComponentGroup from a GroupBuilder.
		/// </summary>
		protected override ValueTask PopulateContent(Context context, ComponentGroup content, ContentBuilder builder)
		{
			if (builder is GroupBuilder groupBuilder)
			{
				content.Name = new Localized<string>(groupBuilder.Name);
				content.AllowedComponents = JsonConvert.SerializeObject(groupBuilder.AllowedComponents);
			}

			return new ValueTask();
		}
	}
      
}
