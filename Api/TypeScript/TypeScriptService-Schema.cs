using Api.AvailableEndpoints;
using Api.CanvasRenderer;
using Api.Contexts;
using Api.Database;
using Api.Revisions;
using Api.Startup;
using Api.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Api.TypeScript
{
	public partial class TypeScriptService : AutoService
	{
		/// <summary>
		/// Creates the schema in to the given container.
		/// </summary>
		/// <param name="container"></param>
		/// <param name="toFileSystem">True if it should also be written to the fs.</param>
		public void CreateApiSchema(SourceFileContainer container, bool toFileSystem)
		{   
			var allContent = _aes.ListByModule();
			var codeSet = new ApiNamespaces();

			var entities = new List<Type>();

			foreach (var moduleEps in allContent)
			{
				var controller = moduleEps.ControllerType;

				if (controller == null)
				{
					continue;
				}

				// Get the namespace (usually e.g. Api.Users):
				var ns = codeSet.Get(controller.Namespace);

				// Require the content type if there is one
				// (the generic class reference makes this happen always, but this is just a guarantee):
				var entityType = moduleEps.GetContentType();

				if (entityType != null)
				{
					if (!entities.Contains(entityType))
					{
						entities.Add(entityType);
					}
					ns.RequireType(entityType);
				}

				CreateControllerClass(moduleEps, codeSet, ns);
			}

			CreateIncludes(codeSet, entities);

			foreach (var kvp in codeSet.Namespaces)
			{
				var ns = kvp.Value;
				var src = ns.ToSource();
				var path = ns.FilePath;

				var file = container.Add(path, src);

				if (toFileSystem)
				{
					// Ensure dir exists:
					var filePath = file.Path;
					var dir = Path.GetDirectoryName(filePath);
					Directory.CreateDirectory(dir);

					// Write:
					File.WriteAllText(filePath, src);
				}
			}
		}

		private void CreateIncludes(ApiNamespaces codeSet, List<Type> entities)
		{
			// Includes
			var includeNs = codeSet.Get("Api.Includes");
			includeNs.Import("ApiIncludes", "UI/Functions/WebRequest");
			var globalIncludes = new ApiClass("ApiGlobalIncludes", "ApiIncludes");
			includeNs.AddType(globalIncludes);

			// Generate virtual field accessors from global virtual fields.
			foreach (var kvp in ContentFields.GlobalVirtualFields)
			{
				var virtualInfo = kvp.Value.VirtualInfo;
				AddVirtualListField(globalIncludes, virtualInfo);
			}

			// Add the wildcard:
			globalIncludes.AddGetProperty("all").Body = "return new ApiIncludes(this.toString(), '*');";

			foreach (var entity in entities)
			{
				RequireIncludesClass(entity, includeNs);
			}

		}

		/// <summary>
		/// No-ops if the specified includeable type was already added.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="includeNs"></param>
		private void RequireIncludesClass(Type entity, ApiNamespace includeNs)
		{
			var incName = entity.Name + "Includes";

			// Already present?
			if (includeNs.ContainsType(incName))
			{
				return;
			}

			var entityIncludes = new ApiClass(incName, "ApiGlobalIncludes");
			includeNs.AddType(entityIncludes);

			var virtuals = entity.GetCustomAttributes<HasVirtualFieldAttribute>();

			foreach (var virtualField in virtuals)
			{
				var property = entityIncludes.AddGetProperty(virtualField.FieldName);

				if (virtualField.Type is null)
				{
					// Functional includes and anything that is not an entity type with its own includes set.
					property.Body = "return new ApiIncludes(this.toString(), '" + virtualField.FieldName.ToLower() + "');";
				}
				else
				{
					var targetType = virtualField.Type.Name;
					property.Body = "return new " + targetType + "Includes(this.toString(), '" + virtualField.FieldName.ToLower() + "');";
				}
			}

			var secondaries = entity.GetCustomAttributes<HasSecondaryResultAttribute>();

			foreach (var secondary in secondaries)
			{
				var property = entityIncludes.AddGetProperty(secondary.FieldName);

				var targetType = secondary.Type.Name;
				property.Body = "return new " + targetType + "Includes(this.toString(), 'secondary." + secondary.FieldName.ToLower() + "');";

				// Ensure it is also generated:
				RequireIncludesClass(secondary.Type, includeNs);
			}
		}

		private void AddVirtualListField(ApiClass onClass, VirtualInfo virtualInfo)
		{
			var name = virtualInfo.FieldName.ToLower();

			Type virtualType = null;

			if (virtualInfo.DynamicTypeField != null)
			{
				virtualType = typeof(object);
			}
			else if (virtualInfo.ValueGeneratorType != null)
			{
				virtualType = virtualInfo.GetValueGeneratorOutputType();
			}
			else
			{
				virtualType = virtualInfo.Type;
			}

			var property = onClass.AddGetProperty(name);

			if (virtualInfo.ValueGeneratorType != null || !TypeScriptService.IsEntityType(virtualType))
			{
				// Functional includes and anything that is not an entity type with its own includes set.
				property.Body = "return new ApiIncludes(this.toString(), '" + TypeScriptService.LcFirst(name) + "');";
			}
			else
			{
				property.Body = "return new " + virtualType.Name + "Includes(this.toString(), '" + TypeScriptService.LcFirst(name) + "');";
			}
		}

		private ApiClass CreateControllerClass(ModuleEndpoints moduleEps, ApiNamespaces codeSet, ApiNamespace ns)
		{
			var entityType = moduleEps.GetContentType();
			var controller = moduleEps.ControllerType;
			var controllerName = controller.Name;

			if (entityType != null)
			{
				ns.RequireType(entityType);
			}

			var cls = ns.RequireType(moduleEps.ControllerType) as ApiClass;

			if(entityType != null)
			{
				// Create an alias if one is needed.
				var entityName = entityType.Name;
				var aliasNs = codeSet.Get("Api." + entityName);

				if (cls.Namespace != aliasNs)
				{
					aliasNs.Import(entityName, ns.ImportPath);
					aliasNs.Import(cls.ExportInstanceAs, ns.ImportPath);
					aliasNs.Append("export {" + entityName + "};\n");
				}

				// Controllers always set ExportInstanceAs to e.g. "UserApi".
				aliasNs.Append("export default " + cls.ExportInstanceAs + ";");
			}

			return cls;
		}

		/// <summary>
		/// True if the given type is one of the main standard numeric types.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsStandardNumericType(Type type)
		{
			return GetStandardNumericTypeName(type) != null;
		}

		/// <summary>
		/// Returns the ts friendly name if the given type is one of the main standard numeric types, or null otherwise.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string GetStandardNumericTypeName(Type type)
		{
			if (type == typeof(byte))
			{
				return "byte";
			}

			if (type == typeof(sbyte))
			{
				return "sbyte";
			}
			
			if(type == typeof(short))
			{
				return "short";
			}

			if(type == typeof(ushort))
			{
				return "ushort";
			}

			if(type == typeof(int))
			{
				return "int";
			}

			if(type == typeof(uint))
			{
				return "uint";
			}

			if(type == typeof(long))
			{
				return "long";
			}

			if(type == typeof(ulong))
			{
				return "ulong";
			}

			if(type == typeof(float))
			{
				return "float";
			}

			if(type == typeof(double))
			{
				return "double";
			}

			if (type == typeof(decimal))
			{
				return "decimal";
			}

			return null;
		}

	}
}