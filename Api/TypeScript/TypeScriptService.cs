using Api.AvailableEndpoints;
using Api.CanvasRenderer;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Api.TypeScript
{
    /// <summary>
    /// Provides functionality to generate TypeScript code based on the .NET API.
    /// This service integrates with <see cref="AvailableEndpointService"/> to
    /// expose endpoint metadata and defines custom mappings and rules for 
    /// the TypeScript code generation process.
    /// </summary>
    public partial class TypeScriptService : AutoService
    {
        /// <summary>
        /// The service used to retrieve information about available API endpoints.
        /// </summary>
        private readonly AvailableEndpointService _aes;

        /// <summary>
        /// Generates the full set of bindings, returning the container that they are in.
        /// </summary>
        public async ValueTask<SourceFileContainer> GenerateAllBindings(Context context, bool toFileSystem = true, string customPath = null)
        {
			if (string.IsNullOrEmpty(customPath))
			{
                // NB: A path is always required even if we're not writing the files out.
				customPath = Path.GetFullPath("TypeScript/Api");
			}

			if (toFileSystem)
			{
				try
				{
                    // attempt to delete the "TypeScript/Api" directory
                    // this will work in all cases EXCEPT first pull of the repo
                    // or when "TypeScript/Api" is manually deleted before 
                    // starting the application.
                    Directory.Delete(customPath, true);
                }
                // don't need to assign a variable to the exception
                // so we just catch it and Log a notice to the log.
                catch (DirectoryNotFoundException)
                {
                    // This is fine - didn't exist anyway.
                }

                // Create if needed (noop otherwise)
                Directory.CreateDirectory(customPath);
            }

			// Create a container to hold the API/*.ts files.
			// It exists such that ultimately the UI bundle compiles files present here as well.
			var container = new SourceFileContainer(customPath, "Api");

			container = await Events.TypeScript.ApiContainer.Dispatch(context, container);

			CreateApiSchema(container, toFileSystem);

            return container;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TypeScriptService"/> class.
		/// Sets up generation rules and creates an API schema representation.
		/// </summary>
		/// <param name="aes">
		/// The <see cref="AvailableEndpointService"/> used to fetch available API endpoint metadata.
		/// </param>
		public TypeScriptService(AvailableEndpointService aes)
        {
            _aes = aes;

            Events.Compiler.BeforeCompile.AddEventListener(async (context, source) =>
            {
				// Create the typescript functionality before the JS is compiled.
				var container = await GenerateAllBindings(context, true);
				
				var uiBundle = source.GetBundle("UI");

				if (uiBundle != null)
				{
					uiBundle.AddContainer(container);
				}

				// local dev uses a Session description:
				ContextGenerator.SaveToFile("TypeScript/Config/Session.tsx");
				GlobalGenerator.GenerateGlobals();

				// Aliases are only used by local dev:
				await BuildTypescriptAliases(context, source.Bundles);

                return source;
            });
            
            Events.Compiler.OnMapChange.AddEventListener(async (context, sourceBuilders) =>
             {
            	// Called when 1 file has changed.
            	// Need to make sure the global.ts file is correct
                // (the C# api won't have changed whilst the api is running, so no other files must regenerate).
            	await BuildTypescriptAliases(context, sourceBuilders);
            
            	return sourceBuilders;
             });
		}

    }
}
