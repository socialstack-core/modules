using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using System;
using Api.Startup;

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

				if (string.IsNullOrEmpty(field.Name))
				{
					// Name required
					throw new PublicException("A field name is required.", "field_name_required");
				}

				var originalName = field.Name;

				var matchingNameCounter = 2;
				var matchingName = await Where("CustomContentTypeId=? AND Name=?", DataOptions.IgnorePermissions).Bind(field.CustomContentTypeId).Bind(field.Name).First(ctx);

				if (matchingName != null)
				{
					field.Name = originalName + matchingNameCounter.ToString();
					matchingName = await Where("CustomContentTypeId=? AND Name=?", DataOptions.IgnorePermissions).Bind(field.CustomContentTypeId).Bind(field.Name).First(ctx);
				}

				while (matchingName != null)
				{
					if (matchingNameCounter >= 99)
					{
						throw new Exception("A field already exists with that name");
					}

					matchingNameCounter++;
					field.Name = originalName + matchingNameCounter.ToString();
					matchingName = await Where("CustomContentTypeId=? AND Name=?", DataOptions.IgnorePermissions).Bind(field.CustomContentTypeId).Bind(field.Name).First(ctx);
				}

				return field;
			});

			Events.CustomContentTypeField.BeforeUpdate.AddEventListener((Context context, CustomContentTypeField field, CustomContentTypeField original) =>
			{
				if (string.IsNullOrEmpty(field.Name))
				{
					throw new PublicException("A field name is required.", "field_name_required");
				}

				return new ValueTask<CustomContentTypeField>(field);
			});

		}
	}
    
}
