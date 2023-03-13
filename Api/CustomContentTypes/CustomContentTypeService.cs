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

			// Example admin page install:
			InstallAdminPages("Data", "fa:fa-database", new string[] { "id", "nickName" });

			Events.Service.AfterStart.AddEventListener(async (Context ctx, object x) =>
			{
				// Get all types:
				var allTypes = await Where("Deleted=?", DataOptions.IgnorePermissions).Bind(false).ListAll(ctx);
				var allTypeFields = await fieldService.Where("Deleted=?", DataOptions.IgnorePermissions).Bind(false).ListAll(ctx);

				// Load them now:
				await LoadCustomTypes(allTypes, allTypeFields);

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
		
			Events.CustomContentType.BeforeCreate.AddEventListener(async (Context ctx, CustomContentType type) => {

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

			Events.CustomContentType.AfterCreate.AddEventListener(async (Context ctx, CustomContentType type) => {

				if (type == null)
				{
					return null;
				}

				await LoadCustomType(ctx, type);

				return type;
			});

			Events.CustomContentType.AfterUpdate.AddEventListener(async (Context ctx, CustomContentType type) => {

				if (type == null)
				{
					return null;
				}

				if (type.Deleted)
                {
					await UnloadCustomType(ctx, type.Id);
				} else
                {
					await LoadCustomType(ctx, type);
				}

				return type;
			});

			Events.CustomContentType.BeforeDelete.AddEventListener(async (Context ctx, CustomContentType type) => {

				var deletedType = await Update(ctx, type, (Context context, CustomContentType toUpdate, CustomContentType original) => {
					toUpdate.Deleted = true;
				});

				return null;
			});

			Events.CustomContentType.AfterDelete.AddEventListener(async (Context ctx, CustomContentType type) => {

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

				if (action == 3 ||type.Deleted)
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

			Events.CustomContentTypeField.AfterCreate.AddEventListener(async (Context ctx, CustomContentTypeField field) => {

				if (field == null)
				{
					return null;
				}

				var type = await Get(ctx, field.CustomContentTypeId, DataOptions.IgnorePermissions);

				await LoadCustomType(ctx, type);

				return field;
			});

			Events.CustomContentTypeField.AfterUpdate.AddEventListener(async (Context ctx, CustomContentTypeField field) => {

				if (field == null)
				{
					return null;
				}

				var type = await Get(ctx, field.CustomContentTypeId, DataOptions.IgnorePermissions);

				await LoadCustomType(ctx, type);

				return field;
			});

			Events.CustomContentTypeField.BeforeDelete.AddEventListener(async (Context ctx, CustomContentTypeField field) => {

				var deletedField = await _fieldService.Update(ctx, field, (Context context, CustomContentTypeField toUpdate, CustomContentTypeField original) => {
					toUpdate.Deleted = true;
				});

				return null;
			});

			Events.CustomContentTypeField.AfterDelete.AddEventListener(async (Context ctx, CustomContentTypeField field) => {

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
				// Shutdown this existing service.
				// This triggers a Delete event internally which 3rd party modules can attach to.
				await Services.StateChange(false, previousCompiledType.Service);
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
			// Create event group for this custom svc:
			var events = new EventGroup<T>();

			// Create the service:
			constructedType.Service = new AutoService<T, uint>(events, constructedType.ContentType,constructedType.ContentType.Name);

			if (loadedTypes == null)
			{
				loadedTypes = new Dictionary<uint, ConstructedCustomContentType>();
			}
			else
			{
				// Does it already exist? If so, we need to remove the existing loaded one.
				await UnloadCustomType(new Context(), constructedType.Id);
			}

			// Add it:
			loadedTypes[constructedType.Id] = constructedType;

			// Register:
			await Services.StateChange(true, constructedType.Service);
		}

	}
    
}
