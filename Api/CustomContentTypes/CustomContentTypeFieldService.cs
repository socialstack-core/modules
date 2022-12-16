using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using System;

namespace Api.CustomContentTypes
{
	/// <summary>
	/// Handles customContentTypeFields.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class CustomContentTypeFieldService : AutoService<CustomContentTypeField>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public CustomContentTypeFieldService() : base(Events.CustomContentTypeField)
        {
			// Example admin page install:
			//InstallAdminPages("Content Type Fields", "fa:fa-rocket", new string[] { "customContentTypeId", "nickName" });

			Events.CustomContentTypeField.BeforeCreate.AddEventListener(async (Context ctx, CustomContentTypeField field) => {

				if (field == null)
				{
					return null;
				}

				if (string.IsNullOrWhiteSpace(field.Name) && !string.IsNullOrWhiteSpace(field.NickName))
				{
					field.Name = TypeEngine.TidyName(field.NickName);
				}

				var matchingField = await Where("CustomContentTypeId=? AND Name=?", DataOptions.IgnorePermissions).Bind(field.CustomContentTypeId).Bind(field.Name).First(ctx);

				if (matchingField != null)
				{
					throw new Exception("A field already exists with that name");
				}

				return field;
			});
		}
	}
    
}
