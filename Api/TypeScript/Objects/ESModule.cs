using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Revisions;
using Api.Startup;
using Api.Users;
using Newtonsoft.Json.Linq;

namespace Api.TypeScript.Objects
{
    /// <summary>
    /// Represents a TypeScript module that contains various generated TypeScript constructs
    /// such as types, enums, controllers, and includes. Responsible for emitting the full
    /// source file content for the module.
    /// </summary>
    public class ESModule : AbstractTypeScriptObject
    {
        // === Fields and Module Members ===

        private string _fileName;

        private readonly List<Import> _normalImports = [];
        private readonly List<WebApis> _usedWebApis = [];
        private readonly List<TypeDefinition> _types = [];
        private readonly List<ESEnum> _enums = [];
        private readonly List<GenericTypeList> _contentTypes = [];
        private readonly List<AutoController> _autoControllers = [];
        private readonly List<NonEntityController> _nonEntityControllers = [];
        private readonly List<ApiIncludes> _apiIncludes = [];
        private readonly List<Type> _virtualFieldImportSymbols = [];
        private readonly HashSet<Type> _typeRegistry = [];
        private bool _isEntityModule = false;
        private Dictionary<string, string> StrictImports = [];

        private static string[] IgnoreNamespaces =
        [
            "Api.Eventing", 
            "Api.WebSockets",
            "Microsoft.AspNetCore",
            "Api.Database"
        ];

        private static Type[] dbContentTypes =
        [
            typeof(Content<>),
            typeof(UserCreatedContent<>),
            typeof(VersionedContent<>),
            typeof(Revision<,>)
        ];

        private static Type[] IgnoreFrameworkTypes =
        [
            typeof(Content),
            typeof(ContentStream<,>),
            typeof(Filter<,>),
            typeof(FilterAst<,>),
            typeof(EventHandler<,>),
            typeof(EndpointEventHandler<,>),
            typeof(JToken),
            typeof(JContainer),
            typeof(ContentStreamSource<,>),
            typeof(EventGroup<,>),
            typeof(AutoService<,>),
            typeof(Context)
        ];

        private EntityController _entityController;
        private TypeScriptService _tsService;

        /// <summary>
        /// The TS Service.
        /// </summary>
        public TypeScriptService TypeScriptService => _tsService;

		 /// <summary>
		 /// Creates a new ESModule running with the given TSService.
		 /// </summary>
		 /// <param name="tsService"></param>
		public ESModule(TypeScriptService tsService)
        {
            _tsService = tsService;
        }

        // === File and Import Metadata ===

        /// <summary>
        /// Sets the output file name for this module.
        /// </summary>
        public void SetFileName(string fileName) => _fileName = fileName;
        
        /// <summary>
        /// Mark this as an entity module
        /// </summary>
        public void MarkAsEntityModule() => _isEntityModule = true;
        
        /// <summary>
        /// True if this is an entity originating module
        /// </summary>
        /// <returns></returns>
        public bool IsEntityModule() => _isEntityModule;

        /// <summary>
        /// Gets the full TypeScript file name for this module.
        /// </summary>
        public string GetFileName() =>
            _fileName + (!_fileName.EndsWith(".ts") && !_fileName.EndsWith(".tsx") ? ".ts" : "");

        /// <summary>
        /// Gets the import path (relative to other TypeScript modules).
        /// </summary>
        public string GetImportPath() => "Api/" + _fileName;

        // === Type/Enum Registration ===

        /// <summary>
        /// Registers a CLR type for TypeScript code generation.
        /// </summary>
        public void AddType(Type type)
        {
            if (type is null)
            {
                return;
            }

            type = TypeScriptService.UnwrapTypeNesting(type);
            
            // filter out some basic items.
            if (type == typeof(void) || type == typeof(ValueTask) ||
                type == typeof(object) || type == typeof(JObject) ||
                type.Namespace == "System")
            {
                return;
            }
            

            if (IgnoreFrameworkTypes.Contains(type) || (type.IsGenericType && IgnoreFrameworkTypes.Contains(type.GetGenericTypeDefinition())))
            {
                return;
            }

            if (dbContentTypes.Contains(type) ||
                (type.IsGenericType && dbContentTypes.Contains(type.GetGenericTypeDefinition())))
            {
                return;
            }

            if (type.Name.Contains("ApiList"))
            {
                return;
            }

            if (_typeRegistry.Contains(type))
            {
                return;
            }

            if (type.IsGenericTypeParameter)
            {
                return;
            }
            
            // prevent any services or any of the Auto functionality
            if (type.IsGenericTypeDefinition)
            {
                if (type.GetGenericTypeDefinition() == typeof(AutoService<>))
                {
                    return;
                }
                if (type.GetGenericTypeDefinition() == typeof(AutoController<>))
                {
                    return;
                }
                if (type.GetGenericTypeDefinition() == typeof(AutoController<,>))
                {
                    return;
                }
                if (type.Name.Contains("Service"))
                {
                    return;
                }
            }

            if (type == typeof(JsonString))
            {
                return;
            }

            foreach (var ns in IgnoreNamespaces)
            {
                if (type.FullName is not null)
                {
                    if (type.FullName.StartsWith(ns))
                    {
                        return;
                    }
                }
            }

            if (TypeScriptService.IsEntityType(type))
            {
                if (IsEntityModule() && _entityController.EntityType != type)
                {
                    return;
                }
            }
            
            
            // Prevent recursive cycles
            if (!_typeRegistry.Add(type)) return;
            
            // this will take the base type through the same conditions.
            if (type.BaseType is not null && _types.All(typeDef => typeDef.GetReferenceType() == type.BaseType))
            {
                var baseType = type.BaseType;

                if (_typeRegistry.Contains(baseType))
                {
                    return;
                }
                AddType(baseType);
            }
            
            _types.Add(new TypeDefinition(type, this));
        }

        /// <summary>
        /// Adds an enum to be emitted as a TypeScript enum.
        /// </summary>
        public void AddEnum(Type type)
        {
            if (_enums.Any(en => en.GetReferenceType() == type)) return;
            _enums.Add(new ESEnum(type, this));
        }
        
        /// <summary>
        /// Adds a non-entity controller.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="includes"></param>
        public void AddNonEntityController(Type type, ESModule includes)
        {
            _nonEntityControllers.Add(new NonEntityController(type, this, includes));
        }

        /// <summary>
        /// Checks if the module already contains a definition for the given type.
        /// </summary>
        public bool HasTypeDefinition(Type type, out TypeDefinition typeDef)
        {
            typeDef = _types.FirstOrDefault(t => t.GetName() == type.Name);
            return typeDef != null;
        }

        // === Imports ===

        /// <summary>
        /// Declares that the current module should import a symbol from another module.
        /// </summary>
        public void Import(string symbol, ESModule from)
        {
            if (from == this) return;

            var existing = _normalImports.FirstOrDefault(i => i.from == from);
            if (existing == null)
            {
                _normalImports.Add(new Import { from = from, Symbols = [symbol] });
            }
            else if (!existing.Symbols.Contains(symbol))
            {
                existing.Symbols.Add(symbol);
            }
        }

        /// <summary>
        /// Declares that the current module should import a type from another module.
        /// </summary>
        public void Import(Type type, ESModule from) => Import(type.Name, from);

        // === Web API Usage ===

        /// <summary>
        /// Marks a web API function as used so that it's imported during generation.
        /// </summary>
        public void RequireWebApi(WebApis webApi) => _usedWebApis.Add(webApi);

        // === Module Composition ===

        /// <summary>
        /// Adds the given generic type info
        /// </summary>
        /// <param name="ctnt"></param>
        public void AddGenericTypes(GenericTypeList ctnt) => _contentTypes.Add(ctnt);

        /// <summary>
        /// Adds the given controller as a generic one
        /// </summary>
        /// <param name="ctrlr"></param>
        public void AddGenericController(AutoController ctrlr) => _autoControllers.Add(ctrlr);

        /// <summary>
        /// Adds an include
        /// </summary>
        /// <param name="include"></param>
        public void AddInclude(ApiIncludes include) => _apiIncludes.Add(include);

        /// <summary>
        /// Sets the given entity controller
        /// </summary>
        /// <param name="entityController"></param>
        public void SetEntityController(EntityController entityController) => _entityController = entityController;

        /// <summary>
        /// sets a controller for the given entity type
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="entity"></param>
        public void SetEntityController(Type controller, Type entity) =>
            SetEntityController(new EntityController(controller, entity, this));

        /// <summary>
        /// Gets the set of required imports
        /// </summary>
        /// <returns></returns>
        public List<Type> GetRequiredImports()
        {
            var required = new List<Type>();
            
            required.AddRange(_nonEntityControllers.SelectMany(ctrller => ctrller.GetRequiredImports()));
            _types.ForEach(typeDef =>
            {
                required.AddRange(typeDef.GetDependencies());
            });

            return required;
        }
        
        // === Output ===

        /// <summary>
        /// Emits the TypeScript source content for this module.
        /// </summary>
        public override void ToSource(StringBuilder builder, TypeScriptService svc)
        {
            if (IsEmpty()) return;

            builder.AppendLine("/**");
            builder.AppendLine(" * This file was automatically generated. DO NOT EDIT.");
            builder.AppendLine(" */\n");

            // Web API imports
            if (_usedWebApis.Count != 0)
            {
                var apis = new List<string> { "ApiList" };

                if (_usedWebApis.Contains(WebApis.GetList)) apis.Add("getList");
                if (_usedWebApis.Contains(WebApis.GetOne)) apis.Add("getOne");
                if (_usedWebApis.Contains(WebApis.GetJson)) apis.Add("getJson");
                apis.Add("getText");

                builder.AppendLine($"import {{ {string.Join(", ", apis)} }} from 'UI/Functions/WebRequest';\n");
            }

            if (IsEntityModule())
            {
                builder.AppendLine("import { ApiIncludes } from 'Api/Includes';");
            }

            // Import requirements
            _types.ForEach(t => {
                t.ImportVirtualFields(svc.modules);
            });
            
            _types.ForEach(type =>
            {
                if (!_normalImports.Any(import => import.Symbols.Contains(type.GetName())))
                {
                    if (IsEntityModule() && type.GetReferenceType() != _entityController.EntityType)
                    {
                        // let's add an import instead
                        var existingItems = svc.modules.Where(module => module.IsEntity((type.GetReferenceType())));
                        var existingModule = existingItems.FirstOrDefault();

                        if (existingModule is not null)
                        {
                            // Import(type.GetName(), existingModule);
                            _typeRegistry.Add(type.GetReferenceType());
                        }
                        return;
                    }
                }
            });

            if (_normalImports.Count > 0)
            {
                builder.AppendLine("// IMPORTS");
                _normalImports.ForEach(i => i.ToSource(builder, svc));
                builder.AppendLine();
            }

            foreach (var entry in StrictImports)
            {
                builder.AppendLine("import {" + entry.Key + "} from '" + entry.Value + "';");
            }

            if (_enums.Count > 0)
            {
                builder.AppendLine("// ENUMS");
                _enums.ForEach(e => e.ToSource(builder, svc));
                builder.AppendLine();
            }

            if (_contentTypes.Count > 0)
            {
                builder.AppendLine("// OPEN GENERICS");
                _contentTypes.ForEach(g => g.ToSource(builder, svc));
                builder.AppendLine();
            }

            builder.AppendLine("// TYPES");
            _types.ForEach(type =>
            {
                if (!_normalImports.Any(import => import.Symbols.Contains(type.GetName())) && !StrictImports.ContainsValue("Api/" + type.GetName()))
                {
                    type.ToSource(builder, svc);
                }
            });

            if (_entityController != null)
            {
                builder.AppendLine("// ENTITY CONTROLLER");
                _entityController.ToSource(builder, svc);
            }

            if (_autoControllers.Count > 0)
            {
                builder.AppendLine("// AUTO CONTROLLERS");
                _autoControllers.ForEach(ac => ac.ToSource(builder, svc));
            }
            
            if (_nonEntityControllers.Count > 0)
            {
                builder.AppendLine("// NON-ENTITY CONTROLLERS");
                _nonEntityControllers.ForEach(ac => ac.ToSource(builder, svc));
            }

            if (_apiIncludes.Count > 0)
            {
                builder.AppendLine("// INCLUDES");
                _apiIncludes.ForEach(inc => inc.ToSource(builder, svc));
            }
        }

        /// <summary>
        /// Creates a new empty module for a given entity and adds it to the given list.
        /// </summary>
        public static ESModule Empty(Type entityType, List<ESModule> modules, TypeScriptService tsService)
        {
            var module = new ESModule(tsService);
            module.SetFileName(entityType.Name);
            modules.Add(module);
            return module;
        }

        /// <summary>
        /// Returns true if the module is empty and does not need to be emitted.
        /// </summary>
        public bool IsEmpty()
        {
            return _normalImports.Count == 0 &&
                   _enums.Count == 0 &&
                   _entityController == null &&
                   _contentTypes.Count == 0 &&
                   _apiIncludes.Count == 0 &&
                   _autoControllers.Count == 0 &&
                   _nonEntityControllers.Count == 0;
        }
        
        /// <summary>
        /// Is the passed type an entity?
        /// </summary>
        /// <param name="virtualType"></param>
        /// <returns></returns>
        public bool IsEntity(Type virtualType)
        {
            return IsEntityModule() && _entityController.EntityType == virtualType;
        }
        
        /// <summary>
        ///  for explicit imports.
        /// </summary>
        /// <param name="virtualTypeName"></param>
        /// <param name="path"></param>
        public void AddRawImport(string virtualTypeName, string path)
        {
            StrictImports[virtualTypeName] = path;
        }
    }

    /// <summary>
    /// Represents the specific Web API utilities that a TypeScript module might need to import.
    /// </summary>
    public enum WebApis
    {
        /// <summary>
        /// Get raw text.
        /// </summary>
        GetText,
        /// <summary>
        /// Get text parsed as JSON. Cannot use includes.
        /// </summary>
        GetJson,
        /// <summary>
        /// Get a singular content object which is JSON formatted and wrapped in {"result":x}. Can use includes.
        /// </summary>
        GetOne,
		/// <summary>
		/// Get a list of content which is JSON formatted and wrapped in {"results":x}. Can use includes.
		/// </summary>
		GetList
	}
}
