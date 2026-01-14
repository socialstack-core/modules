using System;
using System.IO;
using System.Threading.Tasks;
using Api.AvailableEndpoints;
using Api.CanvasRenderer;
using Api.Contexts;
using Api.Database;
using Api.Eventing;

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
        /// Initializes a new instance of the <see cref="TypeScriptService"/> class.
        /// Sets up generation rules and creates an API schema representation.
        /// </summary>
        /// <param name="aes">
        /// The <see cref="AvailableEndpointService"/> used to fetch available API endpoint metadata.
        /// </param>
        public TypeScriptService(AvailableEndpointService aes)
        {
            _aes = aes;

#if DEBUG
            try
            {
                // attempt to delete the "TypeScript/Api" directory
                // this will work in all cases EXCEPT first pull of the repo
                // or when "TypeScript/Api" is manually deleted before 
                // starting the application.
                Directory.Delete("TypeScript/Api", true);
            }
            // don't need to assign a variable to the exception
            // so we just catch it and Log a notice to the log.
            catch (DirectoryNotFoundException)
            {
                Log.Info("TypeScriptService", "Directory not found");
            }
            SetupRules();
            SetupEvents();
            
            Events.Compiler.BeforeCompile.AddEventListener((context, source) =>
            {
                // Create the typescript functionality before the JS is compiled.

                // Create if needed (noop otherwise)
                Directory.CreateDirectory("TypeScript/Api");

                // Create a container to hold the API/*.ts files.
                // It exists such that ultimately the UI bundle compiles files present here as well.
                var container = new SourceFileContainer(Path.GetFullPath("TypeScript/Api"), "Api");

                container = Events.TypeScript.ApiContainer.Dispatch(container);
                
                CreateApiSchema(container);


                var uiBundle = source.GetBundle("UI");

                if (uiBundle != null)
                {
                    uiBundle.AddContainer(container);
                }
                
                BuildTypescriptAliases(source.Bundles);
                
                return ValueTask.FromResult(source);
            });
            
            Events.Compiler.OnMapChange.AddEventListener((ctx, sourceBuilders) =>
             {
            	// Called when 1 file has changed.
            	// Need to make sure the global.ts file is correct
                // (the C# api won't have changed whilst the api is running, so no other files must regenerate).
            	BuildTypescriptAliases(sourceBuilders);
            
            	return ValueTask.FromResult(sourceBuilders);
             });
#endif
		}

		/// <summary>
		/// Configures the rules used during TypeScript code generation.
		/// This includes ignoring specific namespaces and types that should not be included
		/// in the generated TypeScript code.
		/// </summary>
		private void SetupRules()
        {
            // Ignore external or irrelevant namespaces
            IgnoreNamespace("Microsoft.ClearScript");
            IgnoreNamespace("Microsoft.AspNetCore");
            IgnoreNamespace("Newtonsoft.Json");
            IgnoreNamespace("Org.BouncyCastle");
            IgnoreNamespace("MySql.Data");
            IgnoreNamespace("Nest");
            IgnoreNamespace("System");
            IgnoreNamespace("Api.WebSockets");

            // Ignore specific types from Api.Database that are not relevant to TypeScript output
            IgnoreType(typeof(Field));
            IgnoreType(typeof(FieldMap));
            IgnoreType(typeof(AutoService));
            IgnoreType(typeof(ValueType));

            SetupMappings();
            SetupIgnores();
        }

        /// <summary>
        /// Defines type mappings between .NET types and their corresponding TypeScript representations.
        /// This ensures accurate type conversion during code generation.
        /// </summary>
        private void SetupMappings()
        {
            // Numerical type mappings
            SetTypeOverwrite(typeof(byte), "byte");
            SetTypeOverwrite(typeof(sbyte), "sbyte");
            SetTypeOverwrite(typeof(short), "short");
            SetTypeOverwrite(typeof(ushort), "ushort");
            SetTypeOverwrite(typeof(int), "int");
            SetTypeOverwrite(typeof(uint), "uint");
            SetTypeOverwrite(typeof(long), "long");
            SetTypeOverwrite(typeof(ulong), "ulong");
            SetTypeOverwrite(typeof(float), "float");
            SetTypeOverwrite(typeof(double), "double");
            SetTypeOverwrite(typeof(decimal), "double");

            // String and boolean types
            SetTypeOverwrite(typeof(string), "string");
            SetTypeOverwrite(typeof(bool), "boolean");
            
            
            // Custom
            SetTypeOverwrite(typeof(Context), "SessionResponse");
            SetTypeOverwrite(typeof(ValueTask), "void");
            SetTypeOverwrite(typeof(void), "void");
            SetTypeOverwrite(typeof(DateTime), "Date | string | number");
            SetTypeOverwrite(typeof(JsonString), "string");
        }

        private void SetupIgnores()
        {
            AddIgnoreType(typeof(AutoService));
            AddIgnoreType(typeof(AutoService<>));
            AddIgnoreType(typeof(Type));
            AddIgnoreType(typeof(JsonString));
        }
        private void SetupEvents()
        {
            
        }
    }
}
