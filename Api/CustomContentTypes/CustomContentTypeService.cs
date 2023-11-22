using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Contexts;
using Api.Eventing;
using System;
using Api.Startup;
using Api.Pages;

namespace Api.CustomContentTypes
{
    /// <summary>
    /// Handles customContentTypes.
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>
    [LoadPriority(8)]
    public partial class CustomContentTypeService : AutoService<CustomContentType>
    {
        private readonly CustomContentTypeFieldService _fieldService;

        /// <summary>
        /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
        /// </summary>
        public CustomContentTypeService(CustomContentTypeFieldService fieldService) : base(Events.CustomContentType)
        {
            _fieldService = fieldService;

            // NOT using core install admin pages as there are custom/specific page layouts

            // Example admin page install:
            //InstallAdminPages("Data", "fa:fa-database", new string[] { "id", "nickName" });

            Events.Service.AfterStart.AddEventListener((Context context, object sender) =>
            {
                // This route is suggested rather than dependency injection
                // Because some projects (particularly fully headless and micro instances) don't have a page service installed.
                var pageService = Services.Get<PageService>();

                pageService.Install(
                    new Page()
                    {
                        Url = "/en-admin/customcontenttype",
                        Title = "Edit or create custom content types",
                        BodyJson = @"{
	                        ""c"": {
		                        ""t"": ""Admin/Layouts/Datamap"",
		                        ""d"": {
			                        ""endpoint"": ""page"",
			                        ""fields"": [
				                        ""id"",
				                        ""nickName""
			                        ],
			                        ""filter"": {
				                        ""where"": {
					                        ""isForm"": false
				                        }
			                        },
			                        ""singular"": ""Custom Content Type"",
			                        ""plural"": ""custom content types""
		                        },
		                        ""c"": {
			                        ""t"": ""p"",
			                        ""c"": {
				                        ""t"": ""br""
			                        },
			                        ""d"": {},
			                        ""i"": 2
		                        },
		                        ""i"": 3
	                        },
	                        ""i"": 4
                            }"
                    },
                    new Page()
                    {
                        Url = "/en-admin/customcontenttype/{customcontenttype.id}",
                        Title = "Editing custom content type",
                        BodyJson = @"{
	                        ""c"": {
		                        ""t"": ""Admin/Layouts/AutoEdit"",
		                        ""d"": {
			                        ""endpoint"": ""customcontenttype"",
			                        ""singular"": ""Custom Content Type"",
			                        ""id"": ""${primary.id}"",
			                        ""plural"": ""custom content types""
		                        }
	                        }
                        }"
                    },
                    new Page()
                    {
                        Url = "/en-admin/{entity}",
                        Title = "Manage Data Types",
                        BodyJson = @"{
	                        ""c"": {
		                        ""t"": ""Admin/Layouts/List"",
		                        ""d"": {
			                        ""entity"": ""${entity}"",
			                        ""fields"": [
				                        ""id"",
				                        ""name""
			                        ],
			                        ""singular"": ""Data Type"",
			                        ""plural"": ""Data Types"",
			                        ""previousPageUrl"": ""/en-admin/datatypes"",
			                        ""previousPageName"": ""Manage Data Types""
		                        },
		                        ""r"": {
			                        ""beforeList"": {
				                        ""t"": ""p"",
				                        ""c"": {
					                        ""t"": ""br""
				                        },
				                        ""d"": {},
				                        ""i"": 4
			                        },
			                        ""children"": {
				                        ""t"": ""p"",
				                        ""c"": {
					                        ""t"": ""br""
				                        },
				                        ""d"": {},
				                        ""i"": 5
			                        }
		                        },
		                        ""i"": 2
	                        },
	                        ""i"": 3
                            }"
                    },
                    new Page()
                    {
                        Url = "/en-admin/{entity}/{id}",
                        Title = "Editing custom data type",
                        BodyJson = @"{
	                        ""c"": {
		                        ""t"": ""Admin/Layouts/AutoEdit"",
		                        ""d"": {
			                        ""singular"": ""Custom Data Type"",
			                        ""id"": ""${primary.id}"",
			                        ""plural"": ""Custom Data Types"",
			                        ""hideEndpointUrl"": true,
			                        ""previousPageUrl"": ""/en-admin/customcontenttype"",
			                        ""previousPageName"": ""Data""
		                        }
	                        }
                        }"
                    },
                    new Page()
                    {
                        Url = "/en-admin/datatypes",
                        Title = "Manage Data Types",
                        BodyJson = @"{
	                        ""c"": {
		                        ""t"": ""Admin/CustomContentTypeList"",
		                        ""d"": {},
		                        ""i"": 2
	                        },
	                        ""i"": 3
                        }"
                    },
                    new Page()
                    {
                        Url = "/en-admin/forms",
                        Title = "Create or edit forms",
                        BodyJson = @"{
	                        ""c"": {
		                        ""t"": ""Admin/Layouts/List"",
		                        ""d"": {
			                        ""endpoint"": ""customcontenttype"",
			                        ""fields"": [
				                        ""id"",
				                        ""name"",
				                        ""nickName""
			                        ],
			                        ""filter"": {
				                        ""where"": {
					                        ""deleted"": false,
					                        ""isForm"": true
				                        }
			                        },
			                        ""customUrl"": ""forms"",
			                        ""singular"": ""Form"",
			                        ""plural"": ""Forms""
		                        },
		                        ""r"": {
			                        ""beforeList"": {
				                        ""t"": ""p"",
				                        ""c"": {
					                        ""t"": ""br""
				                        },
				                        ""d"": {},
				                        ""i"": 5
			                        },
			                        ""children"": {
				                        ""t"": ""p"",
				                        ""c"": {
					                        ""t"": ""br""
				                        },
				                        ""d"": {},
				                        ""i"": 2
			                        }
		                        },
		                        ""i"": 3
	                        },
	                        ""i"": 4
                        }"
                    },
                    new Page()
                    {
                        Url = "/en-admin/forms/{customcontenttype.id}",
                        Title = "Editing custom form",
                        BodyJson = @"{
	                        ""c"": {
		                        ""t"": ""Admin/Layouts/AutoEdit"",
		                        ""d"": {
			                        ""endpoint"": ""customcontenttype"",
			                        ""singular"": ""Form"",
			                        ""hideEndpointUrl"": true,
			                        ""previousPageUrl"": ""/en-admin/forms/"",
			                        ""previousPageName"": ""Forms"",
			                        ""id"": ""${primary.id}"",
			                        ""plural"": ""forms"",
			                        ""values"": {
				                        ""isForm"": true
			                        },
			                        ""showExportButton"": false
		                        }
	                        }
                        }"
                    }
                    );


                return new ValueTask<object>(sender);
            });

            Events.Service.AfterStart.AddEventListener(async (Context ctx, object x) =>
            {
                // Get all types:
                var allTypes = await Where("Deleted =?", DataOptions.IgnorePermissions).Bind(false).ListAll(ctx);
                var allTypeFields = await fieldService.Where("Deleted=?", DataOptions.IgnorePermissions).Bind(false).ListAll(ctx);

                // Load them now:
                await LoadCustomTypes(allTypes, allTypeFields);

                // NOT using core install admin pages as there are custom/specific page layouts
                // so need to add custom admin nav menu link 
                var navMenuItemService = Api.Startup.Services.Get("AdminNavMenuItemService");

                if (navMenuItemService != null)
                {
                    var installNavMenuEntry = navMenuItemService.GetType().GetMethod("InstallAdminEntry");
                    if (installNavMenuEntry != null)
                    {
                        await (ValueTask)installNavMenuEntry.Invoke(navMenuItemService, new object[] {
                                "/en-admin/" + ServicedType.Name.ToLower(),
                                "fa:fa-database",
                                "Data"
                            });

                        await (ValueTask)installNavMenuEntry.Invoke(navMenuItemService, new object[] {
                                "/en-admin/forms",
                                "fa:fa-file-alt",
                                "Forms"
                            });

                    }
                }

                return x;
            }, 9);

            Events.CustomContentType.BeforeUpdate.AddEventListener((Context context, CustomContentType type, CustomContentType original) =>
            {
                if (string.IsNullOrWhiteSpace(type.Name))
                {
                    throw new PublicException("A name is required", "type_name_required");
                }

                return new ValueTask<CustomContentType>(type);
            });

            Events.CustomContentType.BeforeCreate.AddEventListener(async (Context ctx, CustomContentType type) =>
            {

                if (type == null)
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(type.Name) && !string.IsNullOrWhiteSpace(type.NickName))
                {
                    type.Name = TypeEngine
                                    .TidyName(type.NickName);
                }

                if (string.IsNullOrWhiteSpace(type.Name))
                {
                    throw new PublicException("A name is required", "type_name_required");
                }

                var originalName = type.Name;

                var matchingNameCounter = 2;
                var matchingName = await Where("Name=?", DataOptions.IgnorePermissions).Bind(type.Name).First(ctx);

                if (matchingName != null)
                {
                    type.Name = originalName + matchingNameCounter.ToString();
                    matchingName = await Where("Name=?", DataOptions.IgnorePermissions).Bind(type.Name).First(ctx);
                }

                while (matchingName != null)
                {
                    if (matchingNameCounter >= 99)
                    {
                        throw new Exception("A type already exists with that name");
                    }

                    matchingNameCounter++;
                    type.Name = originalName + matchingNameCounter.ToString();
                    matchingName = await Where("Name=?", DataOptions.IgnorePermissions).Bind(type.Name).First(ctx);
                }

                return type;
            });

            Events.CustomContentType.AfterCreate.AddEventListener(async (Context ctx, CustomContentType type) =>
            {

                if (type == null)
                {
                    return null;
                }

                await LoadCustomType(ctx, type);

                return type;
            });

            Events.CustomContentType.AfterUpdate.AddEventListener(async (Context ctx, CustomContentType type) =>
            {

                if (type == null)
                {
                    return null;
                }

                if (type.Deleted)
                {
                    await UnloadCustomType(ctx, type.Id);
                }
                else
                {
                    await LoadCustomType(ctx, type);
                }

                return type;
            });

            Events.CustomContentType.BeforeDelete.AddEventListener(async (Context ctx, CustomContentType type) =>
            {

                var deletedType = await Update(ctx, type, (Context context, CustomContentType toUpdate, CustomContentType original) =>
                {
                    toUpdate.Deleted = true;
                });

                return null;
            });

            Events.CustomContentType.AfterDelete.AddEventListener(async (Context ctx, CustomContentType type) =>
            {

                if (type == null)
                {
                    return null;
                }

                await UnloadCustomType(ctx, type.Id);

                return type;
            });

            Events.CustomContentType.Received.AddEventListener(async (Context ctx, CustomContentType type, int action) =>
            {
                if (type == null)
                {
                    return null;
                }

                if (action == 3 || type.Deleted)
                {
                    // Deleted
                    await UnloadCustomType(ctx, type.Id);
                }
                else if (action == 1 || action == 2)
                {
                    // Created/ updated on a remote server.
                    await LoadCustomType(ctx, type);
                }

                return type;
            });

            Events.CustomContentTypeField.AfterCreate.AddEventListener(async (Context ctx, CustomContentTypeField field) =>
            {

                if (field == null)
                {
                    return null;
                }

                var type = await Get(ctx, field.CustomContentTypeId, DataOptions.IgnorePermissions);

                await LoadCustomType(ctx, type);

                return field;
            });

            Events.CustomContentTypeField.AfterUpdate.AddEventListener(async (Context ctx, CustomContentTypeField field) =>
            {

                if (field == null)
                {
                    return null;
                }

                var type = await Get(ctx, field.CustomContentTypeId, DataOptions.IgnorePermissions);

                await LoadCustomType(ctx, type);

                return field;
            });

            Events.CustomContentTypeField.BeforeDelete.AddEventListener(async (Context ctx, CustomContentTypeField field) =>
            {

                var deletedField = await _fieldService.Update(ctx, field, (Context context, CustomContentTypeField toUpdate, CustomContentTypeField original) =>
                {
                    toUpdate.Deleted = true;
                });

                return null;
            });

            Events.CustomContentTypeField.AfterDelete.AddEventListener(async (Context ctx, CustomContentTypeField field) =>
            {

                if (field == null)
                {
                    return null;
                }

                var type = await Get(ctx, field.CustomContentTypeId, DataOptions.IgnorePermissions);

                await LoadCustomType(ctx, type);

                return field;
            });
        }

        /// <summary>
        /// Gets the latest generated controller types.
        /// </summary>
        /// <returns></returns>
        public Dictionary<uint, ConstructedCustomContentType> GetGeneratedTypes()
        {
            return loadedTypes;
        }

        /*
		/// <summary>
		/// Loads or reloads the given custom type by one of its fields. If you're doing this for more than one, use LoadCustomTypes instead.
		/// </summary>
		private async Task LoadCustomType(Context context, CustomContentTypeField field)
		{
			var type = await Get(context, field.CustomContentTypeId, DataOptions.IgnorePermissions);

			// Load it:
			await LoadCustomType(context, type);
		}
		*/

        /// <summary>
        /// Unloads a custom type of the given ID.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="typeId"></param>
        /// <returns></returns>
        public async Task UnloadCustomType(Context context, uint typeId)
        {
            // Remove if exists.
            if (loadedTypes.TryGetValue(typeId, out ConstructedCustomContentType previousCompiledType))
            {
                // Shutdown this existing service if it is not native extension.
                if (previousCompiledType.NativeType != null)
                {
                    if (previousCompiledType.Service != null)
                    {
						// Reset instance type:
						await previousCompiledType.Service.SetInstanceType(context, null);
					}
                }
                else
                {
                    // This triggers a Delete event internally which 3rd party modules can attach to.
                    await Services.StateChange(false, previousCompiledType.Service);
                }
            }
        }

        /// <summary>
        /// Loads or reloads the given custom type. If you're doing this for more than one, use LoadCustomTypes instead.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="type"></param>
        public async Task LoadCustomType(Context context, CustomContentType type)
        {
            if (type == null)
            {
                return;
            }

            // Apply the fields:
            type.Fields = await _fieldService.Where("CustomContentTypeId=? AND Deleted=?").Bind(type.Id).Bind(false).ListAll(context);

            // Generate it now:
            var compiledType = TypeEngine.Generate(type);

            // Install it:
            var installMethod = GetType().GetMethod("InstallType");

            var setupType = installMethod.MakeGenericMethod(new Type[] {
                compiledType.ContentType
            });

            setupType.Invoke(this, new object[] { compiledType });

            // Signal a change to MVC:
            ActionDescriptorChangeProvider.Instance.HasChanged = true;
            ActionDescriptorChangeProvider.Instance.TokenSource.Cancel();
        }

        /// <summary>
        /// Loads the set of all types and fields now. This sets up their services and so on.
        /// </summary>
        /// <param name="types"></param>
        /// <param name="fields"></param>
        public async ValueTask LoadCustomTypes(List<CustomContentType> types, List<CustomContentTypeField> fields)
        {
            // Initial content type map:
            var map = new Dictionary<uint, CustomContentType>();

            foreach (var type in types)
            {
                map[type.Id] = type;
                type.Fields = null;
            }

            foreach (var field in fields)
            {
                if (field == null || field.Deleted)
                {
                    continue;
                }

                if (!map.TryGetValue(field.CustomContentTypeId, out CustomContentType contentType))
                {
                    // Old field
                    continue;
                }

                if (contentType.Fields == null)
                {
                    contentType.Fields = new List<CustomContentTypeField>();
                }

                contentType.Fields.Add(field);
            }

            // Build the types:
            var compiledTypes = TypeEngine.Generate(types);

            var installMethod = GetType().GetMethod("InstallType");

            foreach (var compiledType in compiledTypes)
            {
                // Invoke InstallType:
                var setupType = installMethod.MakeGenericMethod(new Type[] {
                    compiledType.ContentType
                });

                await (ValueTask)setupType.Invoke(this, new object[] { compiledType });
            }

            // New controller type - signal it:
            ActionDescriptorChangeProvider.Instance.HasChanged = true;
            ActionDescriptorChangeProvider.Instance.TokenSource.Cancel();
        }

        /// <summary>
        /// Raw controller types for custom types, mapped by CustomContentType.Id -> the constructed result.
        /// </summary>
        private Dictionary<uint, ConstructedCustomContentType> loadedTypes;

        /// <summary>
        /// Creates a service etc for the given system type and activates it. Invoked via reflection with a runtime compiled type.
        /// </summary>
        public async ValueTask InstallType<T>(ConstructedCustomContentType constructedType) where T : Content<uint>, new()
        {
            var ctx = new Context();

			if (constructedType.NativeType != null)
            {
                // This custom type is built on top of another built in type.
                // Rather than instancing a new general-use service, we'll instead tell the
                // active running service that it has a new instance type to use.
                constructedType.Service = Services.GetByContentType(constructedType.NativeType);

				if (constructedType.Service != null)
                {
                    await constructedType.Service.SetInstanceType(ctx, typeof(T));
                }
                else
                {
                    // This happens if:
                    // - A content type with the same name exists
                    // - A service for it does not exist
                    // - It should never actually happen. If it does, something in the API is wrong.
                    Log.Warn("customcontenttype", "Unable to register custom content type " + typeof(T).Name + " due to a service mismatch.");
                    return;
                }
            }
            else
			{
				// Create event group for this custom svc:
				var events = new EventGroup<T>();

				// Create the service:
				constructedType.Service = new AutoService<T, uint>(events, constructedType.ContentType, constructedType.ContentType.Name);

                if (loadedTypes == null)
                {
                    loadedTypes = new Dictionary<uint, ConstructedCustomContentType>();
                }
                else
                {
                    // Does it already exist? If so, we need to remove the existing loaded one.
                    await UnloadCustomType(ctx, constructedType.Id);
                }

                // Add it:
                loadedTypes[constructedType.Id] = constructedType;
            }

			if (constructedType.NativeType == null)
			{
				// Register:
				await Services.StateChange(true, constructedType.Service);  
            }
        }

    }

}
