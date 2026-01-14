using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Api.CanvasRenderer;
using Api.Database;
using Api.Revisions;
using Api.Startup;
using Api.TypeScript.Objects;
using Api.Users;
using Microsoft.AspNetCore.Mvc;

namespace Api.TypeScript
{
    public partial class TypeScriptService : AutoService
    {
        internal List<ESModule> modules = [];
        
        /// <summary>
        /// Creates the schema in to the given container.
        /// </summary>
        /// <param name="container"></param>
        public void CreateApiSchema(SourceFileContainer container)
        {
            // kept in as to not break anything.
            ContextGenerator.SaveToFile("TypeScript/Config/Session.tsx");
            GlobalGenerator.GenerateGlobals();
            
            var allContent = _aes.ListByModule();

            foreach (var module in allContent)
            {
                var entity = module.GetContentType();
                var controller = module.ControllerType;

                if (entity is not null)
                {
                    AddType(entity);
                }

                if (controller is not null)
                {
                    AddType(controller);   
                }
            }
            
            var includesScript = ESModule.Empty(typeof(void), modules);
            includesScript.SetFileName("Includes");

            var includes = new ApiIncludes();
            
            includesScript.AddInclude(includes);
            
            var content = ESModule.Empty(typeof(Content<>), modules);
            
            // this can then be imported via:
            // Api/Content.
            content.SetFileName("Content");

            var generics = new GenericTypeList(content);
            content.AddGenericTypes(generics);
            
            content.RequireWebApi(WebApis.GetList);
            content.RequireWebApi(WebApis.GetJson);
            content.RequireWebApi(WebApis.GetOne);

            generics.AddContentType(typeof(Content<>));
            generics.AddContentType(typeof(UserCreatedContent<>));
            generics.AddContentType(typeof(VersionedContent<>));
            
            content.AddType(typeof(ListFilter));
            generics.AddContentType(typeof(Revision<,>));
            
            content.AddGenericController(new Objects.AutoController(content));
            
            
            GetAllTypes().ForEach(type =>
            {
                if (type.IsEnum)
                {
                    // handle differently.
                    
                    return;
                }

                if (IsControllerDescendant(type))
                {
                    if (IsEntityController(type, out var entityType))
                    {
                        var module = modules.FirstOrDefault(mod => mod.HasTypeDefinition(type, out _)) ?? ESModule.Empty(type, modules);
                        module.SetFileName(entityType.Name);
                        
                        
                        module.Import("Content", content);
                        module.Import("UserCreatedContent", content);
                        module.Import("VersionedContent", content);
                        module.Import("AutoController", content);
                        
                        module.Import(GetGenericSignature(entityType) + "Includes", includesScript);
                        includes.AddEntity(entityType);

                        content.Import("ApiIncludes", includesScript);
                        
                        
                        // create entity.
                        // create controller.
                        // create includes
                        // step 4... free lollipops

                        try
                        {
                            if (!module.HasTypeDefinition(entityType, out _))
                            {
                                module.AddType(entityType);
                            }

                            module.SetEntityController(type, entityType);
                        }
                        catch (NotSupportedException notSupported)
                        {
                            Log.Error("ts/unsupported", notSupported, notSupported.Message);
                        }
                        catch (NullReferenceException nre)
                        {
                            Log.Error("ts/core-error", nre, "An internal error has occured with TypeScript, this is not an error you caused, but one in the underlying typescript module");
                        }
                    }
                    else
                    {
                        try
                        {
                            if (type.BaseType == typeof(ControllerBase) || type.BaseType == typeof(AutoController))
                            {
                                var module = ESModule.Empty(type, modules);
                                module.SetFileName(type.Name);
                                module.AddNonEntityController(type, includesScript);
                            }
                        }
                        catch (NotSupportedException notSupported)
                        {
                            Log.Error("ts/unsupported", notSupported, notSupported.Message);
                        }
                        catch (NullReferenceException nre)
                        {
                            Log.Error("ts/core-error", nre, "An internal error has occured with TypeScript, this is not an error you caused, but one in the underlying typescript module");
                        }
                    }
                }
            });
            
            generics.GetRequiredImports().ForEach(importType =>
            {
                var module = GetOriginOfType(importType);
                content.Import(importType, module);
            });
            
            modules.ForEach(module =>
            {
                
                module.GetRequiredImports().ForEach(importType =>
                {
                    var origin = GetOriginOfType(importType);
                    module.Import(importType, origin);
                });
                
                var builder = new StringBuilder();
                try
                {
                    module.ToSource(builder, this);
                }
                catch (NotSupportedException notSupported)
                {
                    Log.Error("ts/unsupported", notSupported, $"Error creating typescript file: {module.GetFileName()}: {Environment.NewLine}{notSupported.Message}");
                }
                catch (NullReferenceException nre)
                {
                    Log.Error("ts/core-error", nre, "An internal error has occured with TypeScript, this is not an error you caused, but one in the underlying typescript module");
                }

                var src = builder.ToString();
                var path = module.GetFileName();
				container.Add(path, src);
                File.WriteAllText(path, src);
            });


        }

        private ESModule GetOriginOfType(Type importType)
        {
            return IsEntityType(importType) ?
                // we're lookin' for an entity type.
                modules.Find(ext => ext.HasTypeDefinition(importType, out _) && ext.IsEntityModule()) : 
                modules.Find(ext => ext.HasTypeDefinition(importType, out _));
        }
    }
}