using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Api.Database;
using Api.EcmaScript.TypeScript;
using Api.Eventing;
using Api.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Api.EcmaScript
{
    /// <summary>
    /// Handles both JS &amp; TS generation.
    /// </summary>
    public partial class EcmaService : AutoService
    {
        /// <summary>
        /// Used for things like uint => number, int => number, 
        /// </summary>
        private readonly Dictionary<Type, string> TypeConversions = [];
        
        /// <summary>
        /// Kinda ironic this generic huh?
        /// </summary>
        private readonly Dictionary<Type, Script> EntityScriptMapping = [];


        /// <summary>
        /// Constructor
        /// </summary>
        public EcmaService()
        {
            InitTypeConversions();
            CreateBaseApi();
            InitTsScripts();
        }

        private Script GetScriptByEntity(Type entityType)
        {
            if (EntityScriptMapping.TryGetValue(entityType, out Script target))
            {
                return target;
            }
            var sct = new Script();

            EntityScriptMapping[entityType] = sct;
            return sct;
        }

        private void CreateBaseApi()
        {
            var apiScript = GetScriptByEntity(typeof(Content<>));

            apiScript.FileName = "TypeScript/Api/ApiEndpoints.tsx";

            apiScript.AddImport(new() {
                DefaultImport = "webRequest",
                From = "UI/Functions/WebRequest",
                Symbols = ["ApiSuccess", "ApiFailure"]
            });

            // ======== CONTENT.CS =========== \\
            var content = new TypeDefinition() {
                Name = "Content",
                GenericTemplate = "ID"
            };
            AddFieldsToType(typeof(Content<>), content);
            apiScript.AddChild(content);

            // ===== VERSIONEDCONTENT.CS ===== \\
            var versionedContent = new TypeDefinition() {
                Name = "VersionedContent",
                GenericTemplate = "T",
                Inheritence = ["UserCreatedContent<T>"]
            };
            AddFieldsToType(typeof(VersionedContent<>), versionedContent);
            versionedContent.AddProperty("revisionId", "T");
            apiScript.AddChild(versionedContent);

            // ===== USERCREATEDCONTENT.CS ===== \\
            var userGenContent = new TypeDefinition() {
                Name = "UserCreatedContent",
                GenericTemplate = "T",
                Inheritence = ["Content<T>"]
            };
            AddFieldsToType(typeof(UserCreatedContent<>), userGenContent);
            apiScript.AddChild(userGenContent);

            // ===== AutoAPI ===== \\
            var baseControllerClass = new ClassDefinition { 
                Name = "AutoApi",
                GenericTemplate = "EntityType extends VersionedContent<number>"
            };
            var apiUrl = new ClassProperty
            {
                Visibility = "protected",
                PropertyName = "apiUrl",
                PropertyType = "string"
            };
            baseControllerClass.Children.Add(apiUrl);

            // add CRUD methods to controller.

            AddCrudFunctionality(baseControllerClass);

            apiScript.AddChild(baseControllerClass);

            // === SAVING TS FILE === \\

            UpdateScripts();
        }

        /// <summary>
        /// Adds the CRUD Functionality to a class.
        /// </summary>
        /// <param name="baseControllerClass"></param>
        private void AddCrudFunctionality(ClassDefinition baseControllerClass)
        {
            var listMethod = new ClassMethod
            {
                Name = "list",
                ReturnType = "Promise<ApiSuccess<EntityType[]> | ApiFailure>",
                Arguments = [
                    new ClassMethodArgument() {
                        Name = "where",
                        Type = "Partial<Record<keyof(EntityType), string | number | boolean>>",
                        DefaultValue = "{}"
                    },
                    new ClassMethodArgument() {
                        Name = "includes",
                        Type = "string[]",
                        DefaultValue = "[]"
                    }
                ], 
                Injected = [
                    "return webRequest(this.apiUrl + '/list', { where }, { method: 'POST', includes })"
                ]
            };
            var oneMethod = new ClassMethod() {
                Name = "load", 
                ReturnType = "Promise<ApiSuccess<EntityType> | ApiFailure>",
                Arguments = [
                    new ClassMethodArgument() {
                        Name = "id",
                        Type = "number"
                    }
                ],
                Injected = [
                    "return webRequest(this.apiUrl + '/' + id)"
                ]
            };
            var createMethod = new ClassMethod() {
                Name = "create", 
                ReturnType = "Promise<ApiSuccess<EntityType> | ApiFailure>",
                Arguments = [
                    new ClassMethodArgument() {
                        Name = "entity",
                        Type = "EntityType"
                    }
                ],
                Injected = [
                    "return webRequest(this.apiUrl, entity)"
                ]
            };
            var updateMethod = new ClassMethod() {
                Name = "update", 
                ReturnType = "Promise<ApiSuccess<EntityType> | ApiFailure>",
                Arguments = [
                    new ClassMethodArgument() {
                        Name = "entity",
                        Type = "EntityType"
                    }
                ],
                Injected = [
                    "return webRequest(this.apiUrl + '/' + entity.id, entity)"
                ]
            };

            var deleteMethod = new ClassMethod() {
                Name = "delete",
                ReturnType = "Promise<ApiSuccess<EntityType> | ApiFailure>",
                Arguments = [
                    new ClassMethodArgument() {
                        Name = "entityId",
                        Type = "number" 
                    }
                ], 
                Injected = [
                    "return webRequest(this.apiUrl + '/' + entityId, {} , { method: 'DELETE', includes: [] })"
                ]
            };

            var constructorMethod = new ClassMethod() {
                Name = "constructor",
                Arguments = [
                    new ClassMethodArgument() {
                        Name = "apiUrl", 
                        Type = "string"
                    }
                ],
                Injected = [
                    "this.apiUrl = apiUrl;"
                ]
            };
            
            baseControllerClass.Children.Add(constructorMethod);
            baseControllerClass.Children.Add(listMethod);
            baseControllerClass.Children.Add(oneMethod);
            baseControllerClass.Children.Add(createMethod);
            baseControllerClass.Children.Add(updateMethod);
            baseControllerClass.Children.Add(deleteMethod);

        }

        private void AddFieldsToType(Type source, TypeDefinition target)
        {
            foreach (var field in source.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                // Skip compiler-generated backing fields (e.g., auto-properties)
                if (Attribute.IsDefined(field, typeof(CompilerGeneratedAttribute)))
                {
                    continue;
                }
                if (field.IsStatic)
                {
                    continue;
                }
                var fieldName = field.Name;

                if (fieldName[0] == '_')
                {
                    fieldName = fieldName[1..];
                }

                fieldName = LcFirst(fieldName);

                var type = TrimGenericBacktick(GetTypeConversion(field.FieldType));

                if (type == "Nullable")
                {
                    continue;
                }

                // Add only actual fields (not property backers)
                target.AddProperty(fieldName, type);
            }
        }


        private void InitTsScripts()
        {
            

            Events.DevFileGeneration.OnEntityFound.AddEventListener((context, entity) => {
                
                var script = GetScriptByEntity(entity);

                script.FileName = "TypeScript/Api/" + entity.Name + ".tsx";

                script.AddImport(new() {
                    Symbols = ["AutoApi", TrimGenericBacktick(entity.BaseType.Name)],
                    From = "TypeScript/Api/ApiEndpoints"
                });

                var typeDef = new TypeDefinition();
                typeDef.SetName(entity.Name);
                if (entity.BaseType.GenericTypeArguments.Length != 0)
                {
                    typeDef.AddInheritence(TrimGenericBacktick(entity.BaseType.Name) + "<" + GetTypeConversion(entity.BaseType.GenericTypeArguments[0]) + ">");
                }
                AddFieldsToType(entity, typeDef);

                script.AddChild(typeDef);

                UpdateScripts();

                return ValueTask.FromResult(entity);                
            });


            Events.DevFileGeneration.OnControllerFound.AddEventListener((context, controllerType, entityType) => {
                
                if (!controllerType.BaseType.IsGenericType)
                {
                    // support for other controller types coming in future.
                    return ValueTask.FromResult(controllerType);    
                }

                // for now we only allow descendants of AutoController.

                var baseUrl = controllerType.GetCustomAttribute<RouteAttribute>();

                if (baseUrl is null)
                {
                    return ValueTask.FromResult(controllerType);
                }

                var script = GetScriptByEntity(entityType);

                script.FileName = "TypeScript/Api/" + entityType.Name + ".tsx";

                
                var controller = new ClassDefinition() {
                    Name = entityType.Name + "Api",
                    Extends = "AutoApi<" + entityType.Name + ">"
                };

                controller.Children.Add(new ClassMethod() {
                    Name = "constructor", 
                    Injected = [
                        $"super('{baseUrl.Template}')"
                    ]
                });

                script.AddChild(controller);
                
                UpdateScripts();

                return ValueTask.FromResult(controllerType);
            });
        }

        private void UpdateScripts()
        {
            if (!Directory.Exists("TypeScript/Api"))
            {
                Directory.CreateDirectory("TypeScript/Api");
            }

            foreach(var scriptRef in EntityScriptMapping)
            {
                var script = scriptRef.Value;
                File.WriteAllText(script.FileName, script.CreateSource());
            }
        }

        private void InitTypeConversions()
        {
            AddTypeConversion(typeof(string), "string");
            AddTypeConversion(typeof(uint), "number");
            AddTypeConversion(typeof(int), "number");
            AddTypeConversion(typeof(double), "number");
            AddTypeConversion(typeof(float), "number");
            AddTypeConversion(typeof(ulong), "number");
            AddTypeConversion(typeof(long), "number");
            AddTypeConversion(typeof(DateTime), "Date");
            AddTypeConversion(typeof(bool), "boolean");
        }

        /// <summary>
        /// Add a type equivalent for JS for the output.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="jsEquivalent"></param>
        public EcmaService AddTypeConversion(Type t, string jsEquivalent)
        {
            TypeConversions[t] = jsEquivalent;
            return this;
        }

        /// <summary>
        /// Returns the JS equivalent for a CS type, when not known returns unknown
        /// which is an accepted TS keyword.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public string GetTypeConversion(Type t)
        {
            if (TypeConversions.TryGetValue(t, out string jsEquivalent))
            {
                return jsEquivalent;
            }
            return t.Name;
        }

        /// <summary>
        /// Converts entity properties to have LCFirst names.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string LcFirst(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return char.ToLower(input[0]) + input.Substring(1);
        }

        private static string TrimGenericBacktick(string input)
        {
            return input.Contains('`') ? input[..input.IndexOf('`')] : input;
        }
    }
}