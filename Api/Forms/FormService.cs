using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Forms;
using Api.Permissions;
using Api.Startup;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Forms
{
	/// <summary>
	/// Handles forms.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class FormService : AutoService<Form>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public FormService() : base(Events.Form)
        {
			InstallAdminPages("Forms", "fa:fa-th-list", new string[] { "id", "name", "key" });

			Events.Form.BeforeCreate.AddEventListener(async (Context context, Form form) => {

				if (string.IsNullOrEmpty(form.Name))
				{
					throw new PublicException("Name is required", "form/name_required");
				}

				if (string.IsNullOrEmpty(form.Key))
				{
					form.Key = form.Name.ToLower().Trim().Replace(" ", "_");

					var baseKey = form.Key;
					var candidate = baseKey;
					var suffix = 1;

					while (await Where("Key=?", DataOptions.NoCacheIgnorePermissions).Bind(candidate).First(context) != null)
					{
						candidate = baseKey + "_" + suffix;
						suffix++;
					}

					form.Key = candidate;
				}

				return form;
			});

#if !DEBUG
			Cache();
#endif
		}
	}
    
}
