using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using System;
using System.Threading.Tasks;

namespace Api.TypeScript;


/// <summary>
/// Instanced automatically. Handles the 'ts-bindings' command line function.
/// </summary>
[EventListener]
public class StandaloneInit{
	
	/// <summary>
	/// Instanced automatically.
	/// </summary>
	public StandaloneInit()
	{

		Events.Service.CommandLine.AddEventListener(async (Context context, CommandArgs args) => {

			if (args == null || args.Handled)
			{
				return args;
			}

			if (args.Operation != "ts-bindings")
			{
				return args;
			}

			// Includes.ts is reliant on the [ListAs] fields being registered.
			// That happens when we specifically ask services for their content fields.
			// We can't spawn services in this mode though, so we're going to instead loop over valid service types
			// and ask the content fields directly.
			var allServiceTypes = Services.CollectServiceTypes(false);

			foreach (var serviceType in allServiceTypes)
			{
				// Need specifically AutoService<X> types next.
				var autoServiceType = Services.GetAutoServiceType(serviceType);

				if (autoServiceType == null)
				{
					// Not a content autoService type.
					continue;
				}

				var genericArgs = autoServiceType.GetGenericArguments();

				if (genericArgs == null || genericArgs.Length != 2)
				{
					continue;
				}

				// The content type is the first arg:
				var contentType = genericArgs[0];

				// Ensure listAs is registered:
				var cf = new ContentFields(contentType, true);
				cf.RegisterListAs();
			}

			// Generates only the typescript bindings as files, then exits.
			// We can do this by just instancing the TS service and asking it directly.
			var tsService = new TypeScriptService(
				new AvailableEndpoints.AvailableEndpointService()
			);

			// Custom file path if desired:
			var path = args.GetFlag("to", null);
			await tsService.GenerateAllBindings(context, true, path);

			args.Handled = true;
			return args;
		});

	}
}