using Api.Contexts;
using Api.Database;
using Api.Startup;
using Api.Startup.Routing;
using Api.Templates;
using Api.Translate;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Api.TypeScript;


/// <summary>
/// A bucket for C# types which will generate in one file.
/// </summary>
public class ApiNamespace
{
	/// <summary>
	/// The namespace name which always starts with "Api." (e.g. Api.Startup)
	/// </summary>
	public readonly string Namespace;

	/// <summary>
	/// The group of namespaces this one is a part of.
	/// </summary>
	public ApiNamespaces Group;

	/// <summary>
	/// Creates a new API namespace.
	/// </summary>
	/// <param name="ns"></param>
	/// <exception cref="System.Exception"></exception>
	public ApiNamespace(string ns)
	{
		if (ns == null || !ns.StartsWith("Api."))
		{
			throw new Exception("Invalid namespace name: " + ns);
		}

		Namespace = ns;
		ImportPath = Namespace.Replace('.', '/');
		FilePath = ImportPath.Substring(4) + ".ts";
	}

	/// <summary>
	/// If this namespace is nested, then the relative path can include directories.
	/// Api.Startup -> "Api/Startup"
	/// </summary>
	public readonly string ImportPath;

	/// <summary>
	/// ImportPath but with ".ts" and excludes Api/ from the start.
	/// </summary>
	public readonly string FilePath;

	/// <summary>
	/// Types to generate inside this ns.
	/// </summary>
	private List<ApiType> Types = [];

	/// <summary>
	/// Imports in this file.
	/// </summary>
	private List<ApiImport> Imports = [];

	/// <summary>
	/// Adds an import for the given type from the given alias.
	/// </summary>
	/// <param name="typeName"></param>
	/// <param name="fromAlias">E.g. "Api/User".</param>
	public void Import(string typeName, string fromAlias)
	{
		var importer = GetImporter(fromAlias);

		if (!importer.Types.Contains(typeName))
		{
			importer.Types.Add(typeName);
		}
	}

	private StringBuilder _customSource;

	/// <summary>
	/// Append custom source to the end of the namespace content.
	/// </summary>
	/// <param name="src"></param>
	public void Append(string src)
	{
		if (_customSource == null)
		{
			_customSource = new StringBuilder();
		}

		_customSource.Append(src);
	}

	/// <summary>
	/// Will either cause the type to be required by this namespace (if the type is present in it) 
	/// or another namespace. Also handles system types, nullables etc.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public string GetTypeReference(Type type)
	{
		if (type.IsGenericTypeParameter)
		{
			// e.g. 'T' or 'ID'
			return type.Name;
		}

		// System arrays
		if (type.IsArray)
		{
			var eleType = type.GetElementType();
			var rank = type.GetArrayRank();
			var typeStr = GetTypeReference(eleType);

			for (var i = 0; i < rank; i++)
			{
				typeStr += "[]";
			}

			return typeStr;
		}

		if (IsAutoService(type))
		{
			// Special case for all AutoService types.
			// They are occasionally referenced from advanced generic controllers and thus get shorted out to 'any'.
			return "any";
		}

		// If not in a namespace, infer Api.Startup.
		var ns = type.Namespace ?? "Api.Startup";

		// Lists, promises and dictionaries
		if (type.IsGenericType)
		{
			var def = type.GetGenericTypeDefinition();
			var args = type.GetGenericArguments();

			if (def == typeof(List<>) || def == typeof(IEnumerable<>))
			{
				return GetTypeReference(args[0]) + "[]";
			}

			if (def == typeof(ValueTask<>) || def == typeof(Task<>))
			{
				return "Promise<" + GetTypeReference(args[0]) + ">";
			}
			
			if (def == typeof(Dictionary<,>) || def == typeof(SortedDictionary<,>) || def == typeof(ConcurrentDictionary<,>))
			{
				// Null not permitted in the key of a Record (it would need to be constructed as a Map instead).
				var keyType = args[0] == typeof(string) ? "string" : GetTypeReference(args[0]);

				return "Record<" + keyType + "," + GetTypeReference(args[1]) + ">";
			}

			if (def == typeof(ContentStream<,>))
			{
				return "ApiList<" + GetTypeReference(args[0]) + ">";
			}

			if (def == typeof(Localized<>))
			{
				return GetTypeReference(args[0]);
			}

			if (def == typeof(Nullable<>))
			{
				return GetTypeReference(args[0]) + "|null";
			}

			if (!ns.StartsWith("Api."))
			{
				throw new NotImplementedException("Unable to emit generic type: " + type.FullName);
			}

			// Is the namespace the same as this?
			// if so, we require the type.
			// Otherwise, it'll be imported and required in the remote namespace.
			var gHostNs = Group.Get(ns);
			var structuredGenType = gHostNs.RequireType(def);

			if (gHostNs != this)
			{
				// Import it
				Import(structuredGenType.Name, gHostNs.ImportPath);
			}

			var typeStr = structuredGenType.Name + "<";

			for (var i = 0; i < args.Length; i++)
			{
				if (i > 0)
				{
					typeStr += ",";
				}

				typeStr += GetTypeReference(args[i]);
			}

			typeStr += ">";

			return typeStr;
		}

		// Nullables
		var nullable = Nullable.GetUnderlyingType(type);

		if (nullable != null)
		{
			return GetTypeReference(nullable) + "?";
		}

		// Core system types - int, bool etc.
		if (ns == "System" || ns.StartsWith("System."))
		{
			if (type == typeof(bool))
			{
				return "boolean";
			}

			if (type == typeof(decimal))
			{
				return "double";
			}
			
			if (type == typeof(void))
			{
				return "void";
			}

			if (type == typeof(ValueTask) || type == typeof(Task))
			{
				return "Promise<void>";
			}

			if (type == typeof(DateTime))
			{
				return "Dateish";
			}

			if (type == typeof(DateOnly))
			{
				return "Dateish";
			}

			if (type == typeof(Guid))
			{
				return "string";
			}

			if (type == typeof(DayOfWeek))
			{
				// 0 (sunday) to 6 (sat)
				return "int";
			}

			// int, uint, short, ..
			var stdNumericName = TypeScriptService.GetStandardNumericTypeName(type);

			if (stdNumericName != null)
			{
				return stdNumericName;
			}

			if (type == typeof(string))
			{
				return "(string|null)";
			}

			if (type == typeof(object))
			{
				return "any";
			}

			throw new NotImplementedException("unknown system type: " + type);
		}

		// Raw JSON:
		if (type == typeof(JObject) || type == typeof(JToken) || type == typeof(JArray))
		{
			return "any";
		}

		if (!ns.StartsWith("Api."))
		{
			throw new NotImplementedException("Unable to emit type: " + type.FullName);
		}

		// Specials
		if (type == typeof(JsonString))
		{
			return "string";
		}

		if (type == typeof(Context))
		{
			return "SessionResponse";
		}

		if (type == typeof(FileContent))
		{
			return "Blob";
		}

		// Is the namespace the same as this?
		// if so, we require the type.
		// Otherwise, it'll be imported and required in the remote namespace.
		var hostNs = Group.Get(ns);
		var structuredType = hostNs.RequireType(type);

		if (hostNs != this)
		{
			// Import it
			Import(structuredType.Name, hostNs.ImportPath);
		}

		return structuredType.Name;
	}

	/// <summary>
	/// True if the given type is a blob compatible type (byte[] or FileContent).
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public static bool IsBlob(Type type)
	{
		type = UnwrapTask(type);
		var nullable = Nullable.GetUnderlyingType(type);

		if (nullable != null)
		{
			type = nullable;
		}

		return (type == typeof(byte[]) || type == typeof(FileContent));
	}

	/// <summary>
	/// True if the given type is outputted as JSON by the routing system.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public static bool IsJsonOutput(Type type)
	{
		type = UnwrapTask(type);
		var nullable = Nullable.GetUnderlyingType(type);

		if (nullable != null)
		{
			type = nullable;
		}

		if (!type.IsValueType)
		{
			return true;
		}

		return type != typeof(string);
	}

	/// <summary>
	/// True if the given type is a ContentStream.
	/// </summary>
	/// <param name="type"></param>
	/// <param name="streamOfType"></param>
	/// <returns></returns>
	public static bool IsContentStream(Type type, out Type streamOfType)
	{
		type = UnwrapTask(type);
		var nullable = Nullable.GetUnderlyingType(type);

		if (nullable != null)
		{
			type = nullable;
		}

		if (!type.IsGenericType)
		{
			streamOfType = null;
			return false;
		}

		if (type.GetGenericTypeDefinition() == typeof(ContentStream<,>))
		{
			streamOfType = type.GetGenericArguments()[0];
			return true;
		}

		streamOfType = null;
		return false;
	}

	/// <summary>
	/// Unwraps Task/ValueTask from the given type.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public static Type UnwrapTask(Type type)
	{
		if (type == typeof(ValueTask) || type == typeof(Task))
		{
			return typeof(void);
		}

		if (type.IsGenericType)
		{
			var def = type.GetGenericTypeDefinition();

			if (def == typeof(ValueTask<>) || def == typeof(Task<>))
			{
				var args = type.GetGenericArguments();
				return args[0];
			}
		}

		return type;
	}

	/// <summary>
	/// Strips the backtick segment of generic type names.
	/// </summary>
	/// <param name="typeName"></param>
	/// <returns></returns>
	public static string TidyGenericName(string typeName)
	{
		var backtick = typeName.IndexOf('`');

		if (backtick != -1)
		{
			return typeName[0..backtick];
		}

		return typeName;
	}

	/// <summary>
	/// Adds a default import from the given alias.
	/// </summary>
	/// <param name="defaultName"></param>
	/// <param name="fromAlias">E.g. "Api/User".</param>
	public void ImportDefault(string defaultName, string fromAlias)
	{
		var importer = GetImporter(fromAlias);
		importer.NamedDefault = defaultName;
	}

	/// <summary>
	/// Get/ create importer for given alias.
	/// </summary>
	/// <param name="fromAlias"></param>
	/// <returns></returns>
	private ApiImport GetImporter(string fromAlias)
	{
		var importer = Imports.Find(importer => importer.FromAlias == fromAlias);

		if (importer == null)
		{
			importer = new ApiImport(fromAlias);
			Imports.Add(importer);
		}

		return importer;
	}

	private Dictionary<Type, ApiType> _uniqueTypes = new Dictionary<Type, ApiType>();

	private bool IsAutoController(Type type)
	{
		if (type == null || type.IsEnum || type.IsValueType || type == typeof(object))
		{
			return false;
		}

		if (type == typeof(AutoController))
		{
			return true;
		}

		return IsAutoController(type.BaseType);
	}

	private bool IsAutoService(Type type)
	{
		if (type == null || type.IsEnum || type.IsValueType || type == typeof(object))
		{
			return false;
		}

		if (type == typeof(AutoService))
		{
			return true;
		}

		return IsAutoService(type.BaseType);
	}

	private bool IsContentType(Type type)
	{
		if (type == null || type.IsEnum || type.IsValueType || type == typeof(object))
		{
			return false;
		}

		if (type == typeof(Content))
		{
			return true;
		}

		return IsContentType(type.BaseType);
	}

	/// <summary>
	/// Requires this file to emit the given type.
	/// </summary>
	/// <param name="type"></param>
	public ApiType RequireType(Type type)
	{
		if (_uniqueTypes.TryGetValue(type, out ApiType result))
		{
			return result;
		}

		var name = TidyGenericName(type.Name);
		var nameOverride = type.GetCustomAttribute<JsonTypeNameAttribute>();

		if (nameOverride != null)
		{
			name = nameOverride.Name;
		}

		// If it is any type of AutoController then it is an ApiClass.
		ApiType structuredType = null;
		var loadControllerMethods = false;

		if (IsAutoController(type))
		{
			loadControllerMethods = true;
			structuredType = new ApiClass(name);

			// If it's specifically the base-most type, AutoController, then add a field.
			if (type == typeof(AutoController))
			{
				structuredType.AddField("ep", "string", false, "\"\"");
			}
			else if (!type.IsGenericType)
			{
				// Concrete controllers can have [Route("..")] which states a base path to use.
				// So that generic controllers use the correct base path, we need to define a ctor which
				// sets the above 'ep' field to that [Route] value from the controller.
				// Methods on generic controllers always concat ep to their URLs.
				var routeAttr = type.GetCustomAttribute<RouteAttribute>();
				var structuredClass = (ApiClass)structuredType;
				var ctor = structuredClass.AddMethod("constructor");
				ctor.Body = "super();";

				if (routeAttr != null && !string.IsNullOrEmpty(routeAttr.Template))
				{
					var template = routeAttr.Template;

					ctor.Body += "\n\t\tthis.ep=\"";

					if (template.StartsWith("v1/"))
					{
						template = template.Substring(3);
					}else if (template.StartsWith("/v1/"))
					{
						template = template.Substring(4);
					}

					if (!template.EndsWith("/"))
					{
						template += "/";
					}

					ctor.Body += template.ToLower() + "\";";
				}

				var entityType = GetControllerEntityType(type.BaseType);

				if (entityType != null)
				{
					var incName = entityType.Name + "Includes";
					structuredType.AddField("includes", incName, false, null, "public");
					ctor.Body += "\n\t\tthis.includes = new " + incName + "();";
					Import(incName, "Api/Includes");
				}

				var exportAs = type.Name;

				if (exportAs.ToLower().EndsWith("controller"))
				{
					exportAs = exportAs.Substring(0, exportAs.Length - 10);
				}

				structuredClass.ExportInstanceAs = exportAs + "Api";
			}

		}
		else if (type.IsEnum)
		{
			structuredType = new ApiEnum(name);
		}
		else
		{
			structuredType = new ApiType(name);
		}

		_uniqueTypes[type] = structuredType;

		// Has it got a base type that isn't object/ valuetype?
		if (!type.IsEnum && type.BaseType != typeof(object) && type.BaseType != typeof(ValueType))
		{
			structuredType.BaseType = GetTypeReference(type.BaseType);
		}

		// Important: Add structuredType *after* baseType. _uniqueTypes blocks infinite recursion.
		// This guarantees the deps of ApiClass occur in the correct order.
		AddType(structuredType);

		if (type.IsGenericTypeDefinition)
		{
			var genericArgs = type.GetGenericArguments();
			structuredType.GenericNames = new List<string>();

			for (var i = 0; i < genericArgs.Length; i++)
			{
				structuredType.GenericNames.Add(genericArgs[i].Name);
			}

		}

		if (type.IsEnum)
		{
			var enumFields = type.GetEnumNames();
			var enumVals = type.GetEnumValuesAsUnderlyingType();

			for (var i = 0; i < enumFields.Length; i++)
			{
				var value = enumVals.GetValue(i);
				structuredType.AddField(enumFields[i], "int", false, value.ToString());
			}
		}
		else if (loadControllerMethods)
		{
			// AutoController
			AddControllerMethods(type, (ApiClass)structuredType);
		}
		else if(type.Namespace != null)
		{
			// Collect the fields & properties.
			var publicFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			var publicProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

			foreach (var field in publicFields)
			{
				var jsonIgnore = field.GetCustomAttribute<JsonIgnoreAttribute>();

				if (jsonIgnore != null)
				{
					continue;
				}

				var json2 = field.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>();
				if (json2 != null)
				{
					Log.Warn("type", "Incorrect JsonIgnore attribute in use on " + field.Name + " on type " + type.Name + " (use Newtonsoft).");
					continue;
				}

				var options = field.GetCustomAttribute<JsonOptionsAttribute>();
				AddTypeField(structuredType, field.Name, field.FieldType, options);
			}

			foreach (var property in publicProperties)
			{
				var jsonIgnore = property.GetCustomAttribute<JsonIgnoreAttribute>();

				if (jsonIgnore != null)
				{
					continue;
				}

				var json2 = property.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>();
				if (json2 != null)
				{
					Log.Warn("type", "Incorrect JsonIgnore attribute in use on " + property.Name + " on type " + type.Name + " (use Newtonsoft).");
					continue;
				}

				var options = property.GetCustomAttribute<JsonOptionsAttribute>();
				AddTypeField(structuredType, property.Name, property.PropertyType, options);
			}

			var virtuals = type.GetCustomAttributes<HasVirtualFieldAttribute>(false);

			foreach (var virtualField in virtuals)
			{
				AddTypeField(structuredType, virtualField.FieldName, virtualField.Type ?? typeof(object), null);
			}

			if (type == typeof(Content))
			{
				// Root content type - add all the listAs fields to it.
				foreach (var kvp in ContentFields.GlobalVirtualFields)
				{
					var virtField = kvp.Value;
					var vInfo = virtField.VirtualInfo;
					if (vInfo == null || (vInfo.IsExplicit && vInfo.ImplicitTypes != null && vInfo.ImplicitTypes.Count > 0))
					{
						continue;
					}

					var fieldName = TypeScriptService.LcFirst(virtField.Name);
					Type fieldType = null;

					if (virtField.VirtualInfo.IsList)
					{
						fieldType = virtField.VirtualInfo.Type;
					}
					else if (virtField.VirtualInfo.ValueGeneratorType != null)
					{
						fieldType = virtField.VirtualInfo.GetValueGeneratorOutputType();
					}

					if (fieldType == null)
					{
						continue;
					}

					// Will intentionally crash if the type is unsupported.
					var typeRef = GetTypeReference(fieldType);
					var field = structuredType.AddField(fieldName, virtField.VirtualInfo.IsList ? typeRef + "[]" : typeRef);
					field.Optional = true;
				}
			}
			else if(IsContentType(type))
			{
				// Search for any implicits if this is a content type
				foreach (var kvp in ContentFields.GlobalVirtualFields)
				{
					var virtField = kvp.Value;
					var vInfo = virtField.VirtualInfo;
					if (vInfo == null || !vInfo.IsExplicit || vInfo.ImplicitTypes == null || vInfo.ImplicitTypes.Count == 0)
					{
						continue;
					}

					var present = false;

					foreach (var impl in vInfo.ImplicitTypes)
					{
						if (impl == type)
						{
							present = true;
							break;
						}
					}

					if (!present)
					{
						continue;
					}

					var fieldName = TypeScriptService.LcFirst(virtField.Name);
					Type fieldType = null;

					if (virtField.VirtualInfo.IsList)
					{
						fieldType = virtField.VirtualInfo.Type;
					}
					else if (virtField.VirtualInfo.ValueGeneratorType != null)
					{
						fieldType = virtField.VirtualInfo.GetValueGeneratorOutputType();
					}

					if (fieldType == null)
					{
						continue;
					}

					// Will intentionally crash if the type is unsupported.
					var typeRef = GetTypeReference(fieldType);
					var field = structuredType.AddField(fieldName, virtField.VirtualInfo.IsList ? typeRef + "[]" : typeRef);
					field.Optional = true;
				}
			}
		}

		return structuredType;
	}

	/// <summary>
	/// Gets the entity type e.g. User from the given controller base type. 
	/// It's almost always the 1st generic arg, but there can be custom controllers where it is not.
	/// </summary>
	/// <param name="controllerBaseType"></param>
	/// <returns></returns>
	private Type GetControllerEntityType(Type controllerBaseType)
	{
		if (controllerBaseType == null || controllerBaseType.IsValueType || controllerBaseType == typeof(object))
		{
			return null;
		}

		if (!controllerBaseType.IsGenericType)
		{
			return GetControllerEntityType(controllerBaseType.BaseType);
		}

		var genericArgs = controllerBaseType.GetGenericArguments();

		for (var i = 0; i < genericArgs.Length; i++)
		{
			if (TypeScriptService.IsEntityType(genericArgs[i]))
			{
				return genericArgs[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Adds controller methods for the given controller type to the given structured class.
	/// </summary>
	/// <param name="controllerType"></param>
	/// <param name="ctrlClass"></param>
	/// <exception cref="Exception"></exception>
	public void AddControllerMethods(Type controllerType, ApiClass ctrlClass)
	{
		var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

		var mainRouteAttr = controllerType.GetCustomAttribute<RouteAttribute>();
		var mainRoute = mainRouteAttr == null ? "" : mainRouteAttr.Template;

		if (string.IsNullOrEmpty(mainRoute))
		{
			mainRoute = "";
		}

		foreach (var ctrlMethod in methods)
		{
			var allAttribs = ctrlMethod.GetCustomAttributes();

			if (allAttribs == null)
			{
				continue;
			}

			// Looking for HttpGet/ HttpPut etc.
			foreach (var attrib in allAttribs)
			{
				if (attrib is HttpGetAttribute hg)
				{
					AddControllerMethod(ctrlMethod, mainRoute, hg.Template, "GET", ctrlClass);
				}
				else if (attrib is HttpPutAttribute hp)
				{
					AddControllerMethod(ctrlMethod, mainRoute, hp.Template, "PUT", ctrlClass);
				}
				else if (attrib is HttpDeleteAttribute hd)
				{
					AddControllerMethod(ctrlMethod, mainRoute, hd.Template, "DELETE", ctrlClass);
				}
				else if (attrib is HttpPostAttribute hpo)
				{
					AddControllerMethod(ctrlMethod, mainRoute, hpo.Template, "POST", ctrlClass);
				}
			}
		}
	}

	private void AddControllerMethod(MethodInfo ctrlMethod, string mainRoute, string template, string httpMethod, ApiClass ctrlClass)
	{
		if (string.IsNullOrEmpty(template))
		{
			template = "";
		}

		string url;

		if (string.IsNullOrEmpty(template))
		{
			url = mainRoute;
		}
		else
		{
			if (template.StartsWith("/"))
			{
				template = template.Substring(1);
			}

			if (string.IsNullOrEmpty(mainRoute))
			{
				url = template;
			}
			else
			{
				url = mainRoute + "/" + template;
			}
		}

		var rawName = ctrlMethod.Name;
		var lcFirstName = TypeScriptService.LcFirst(rawName);
		var method = ctrlClass.AddMethod(lcFirstName);

		var onAGenericController = ctrlMethod.DeclaringType.IsGenericType;

		var parameters = ctrlMethod.GetParameters();
		ApiMethodParameter fromBodyParameter = null;

		foreach (var parameter in parameters)
		{
			// If the type originates from the Api.* namespaces, add it as a required type (minus a few exceptions - Context etc).

			var paramType = parameter.ParameterType;

			if ((paramType.Namespace != null && paramType.Namespace.StartsWith("Microsoft.")) || paramType == typeof(Context)) // HttpContext etc, plus Context itself.
			{
				continue;
			}

			// Does it have a default value?
			var hasDefaultVal = parameter.HasDefaultValue;

			var methodParam = method.AddParameter(parameter.Name.ToLower(), GetTypeReference(paramType));

			if (parameter.GetCustomAttribute<FromBodyAttribute>() != null)
			{
				fromBodyParameter = methodParam;
			}

			if (hasDefaultVal)
			{
				var val = parameter.DefaultValue;

				if (val == null)
				{
					methodParam.DefaultValue = "null";
				}
				else
				{
					var defaultType = val.GetType();
					var dtNullable = Nullable.GetUnderlyingType(defaultType);

					if (dtNullable != null)
					{
						defaultType = dtNullable;
					}

					if (defaultType == typeof(string))
					{
						methodParam.DefaultValue = "\"" + defaultType.ToString() + "\"";
					}
					else if (defaultType == typeof(bool))
					{
						methodParam.DefaultValue = ((bool)val) ? "true" : "false";
					}
					else if (TypeScriptService.IsStandardNumericType(defaultType))
					{
						methodParam.DefaultValue = "(" + val.ToString() + " as " + methodParam.Type + ")";
					}
					else
					{
						throw new Exception("Unknown default value type: " + defaultType);
					}
				}
			}
		}

		var rawReturnType = ctrlMethod.ReturnType;
		var returnType = rawReturnType;

		// Check for a [Returns] attribute for instances where an endpoint generates raw JSON.
		var returnTypeOverrideAttr = ctrlMethod.GetCustomAttribute<ReturnsAttribute>();
		if (returnTypeOverrideAttr != null)
		{
			returnType = returnTypeOverrideAttr.ReturnType;
		}

		var tasklessReturnType = UnwrapTask(returnType);
		var returnsContext = false;

		if (tasklessReturnType == typeof(Context))
		{
			// Special case for returning Context: setSession must be given as the 1st parameter.
			method.InsertParameter(0, "setSession", "(s: SessionResponse) => Session");
			returnsContext = true;
		}

		// If it is a ContentStream type (incl. a nullable one) then 
		// use getList.
		var funcToCall = "";
		Type includesForType = null;

		if (IsContentStream(tasklessReturnType, out Type streamOfType))
		{
			includesForType = streamOfType;
			funcToCall = "getList<" + GetTypeReference(streamOfType) + ">";
			Import("getList", "UI/Functions/WebRequest");
		}
		else if (IsBlob(tasklessReturnType))
		{
			funcToCall = "getBlob";
			Import("getBlob", "UI/Functions/WebRequest");
		}
		else if (IsJsonOutput(tasklessReturnType) && tasklessReturnType != typeof(void))
		{
			// Is it a content type?
			if (TypeScriptService.IsEntityType(tasklessReturnType))
			{
				includesForType = tasklessReturnType;
				funcToCall = "getOne<" + GetTypeReference(tasklessReturnType) + ">";

				Import("getOne", "UI/Functions/WebRequest");
			}
			else
			{
				funcToCall = "getJson<" + GetTypeReference(tasklessReturnType) + ">";
				Import("getJson", "UI/Functions/WebRequest");
			}
		}
		else
		{
			funcToCall = "getText";
			Import("getText", "UI/Functions/WebRequest");
		}

		if (url.StartsWith("v1/"))
		{
			url = url.Substring(3);
		}

		if (url.StartsWith("/v1/"))
		{
			url = url.Substring(4);
		}

		if (url.Length > 0)
		{
			if (url[0] == '{')
			{
				// Starts with a variable
				url = url.Substring(1);
			}
			else
			{
				url = "\"" + url;
			}

			if (url[url.Length - 1] == '}')
			{
				// Ends with a variable
				url = url.Substring(0, url.Length - 1);
			}
			else
			{
				url += "\"";
			}
		}
		else
		{
			url = "\"\"";
		}

		// Note: This toLower lowercases variables. They are all lowercased further up as well.
		url = url.Replace("{", "\" + ").Replace("}", " + \"").ToLower();

		if ((httpMethod == "POST" || httpMethod == "PUT") && fromBodyParameter == null)
		{
			// Must be added before the optional includes one.
			method.AddParameter("bodyContent", "Blob");
		}

		if (includesForType != null)
		{
			method.AddParameter("includes", "ApiInclude[]").IsOptional = true;

			// ?includes= or &includes=.
			url += " + includeString(includes" + (url.Contains("?") ? ",false" : "") + ")";
			Import("includeString", "UI/Functions/WebRequest");
			Import("ApiInclude", "UI/Functions/WebRequest");
		}

		var body = "return " + funcToCall + "(";

		if (onAGenericController)
		{
			body += "this.ep+";
		}

		body += url;

		if (httpMethod == "GET")
		{
			// Nothing else needed.
		}
		else if (httpMethod == "POST" || httpMethod == "PUT")
		{
			// The payload arg is located via the [FromBody] attribute.
			// If not specified, it is assumed the body is handled via a custom stream 
			// and thus an automatic "bodyContent" param is inserted.

			body += ",";

			if (fromBodyParameter != null)
			{
				body += fromBodyParameter.Name;
			}
			else
			{
				body += "bodyContent";
			}

			if (httpMethod == "PUT")
			{
				body += ",{method: \"PUT\"}";
			}
		}
		else if (httpMethod == "DELETE")
		{
			// No payload:
			body += ",undefined,{method: \"DELETE\"}";
		}

		body += ")";

		if (returnsContext)
		{
			body += ".then(setSession)";
		}

		method.Body = body + ";";
	}

	private ApiTypeField AddTypeField(ApiType targetType, string casedName, Type valueType, JsonOptionsAttribute options)
	{
		var fieldName = TypeScriptService.LcFirst(casedName);

		// Console.WriteLine("Field " + casedName + ":" + targetType.Name + " is " + valueType);

		if (options?.PublicType != null)
		{
			// Ensure this type is present in the public TS bindings.
			GetTypeReference(options.PublicType);
		}

		// Will intentionally crash if the type is unsupported.
		var typeRef = GetTypeReference(valueType);

		var field = targetType.AddField(fieldName, typeRef);
		field.Optional = options != null && options.Optional;
		return field;
	}

	/// <summary>
	/// Adds a type which will be generated inside this file.
	/// </summary>
	/// <param name="type"></param>
	public void AddType(ApiType type)
	{
		type.Namespace = this;
		Types.Add(type);
	}

	/// <summary>
	/// Adds a type which will be generated inside this file.
	/// </summary>
	/// <param name="name"></param>
	public bool ContainsType(string name)
	{
		foreach (var type in Types)
		{
			if (type.Name == name)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Generates typescript source for this module.
	/// </summary>
	/// <returns></returns>
	public string ToSource()
	{
		StringBuilder builder = new StringBuilder();

		// Import statements:
		foreach (var import in Imports)
		{
			builder.Append("import ");

			var hasDefault = false;

			if (!string.IsNullOrEmpty(import.NamedDefault))
			{
				hasDefault = true;
				builder.Append(import.NamedDefault);
			}

			if (import.Types != null && import.Types.Count > 0)
			{
				if (hasDefault)
				{
					builder.Append(',');
				}
				builder.Append('{');

				var firstType = true;

				foreach (var importedType in import.Types)
				{
					if (firstType)
					{
						firstType = false;
					}
					else
					{
						builder.Append(',');
					}

					builder.Append(importedType);
				}

				builder.Append('}');
			}

			builder.Append(" from '");
			builder.Append(import.FromAlias);
			builder.Append("';\n");
		}

		// Notice:
		builder.AppendLine("// File is generated. DO NOT EDIT.\n");

		// Any types (or classes):
		foreach (var type in Types)
		{
			type.ToSource(builder);
		}

		// And any custom source:
		if (_customSource != null)
		{
			builder.Append(_customSource);
		}

		return builder.ToString();
	}

}

/// <summary>
/// A set of namespaces in the API.
/// </summary>
public class ApiNamespaces
{
	private Dictionary<string, ApiNamespace> _namespaces = new();

	/// <summary>
	/// Get/create the given namespace.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public ApiNamespace Get(string name)
	{
		if (!_namespaces.TryGetValue(name, out ApiNamespace ns))
		{
			ns = new ApiNamespace(name);
			ns.Group = this;
			_namespaces[name] = ns;
		}

		return ns;
	}

	/// <summary>
	/// The namespaces in the set.
	/// </summary>
	public Dictionary<string, ApiNamespace> Namespaces => _namespaces;
}

/// <summary>
/// A parameter in an API method.
/// </summary>
public class ApiMethodParameter
{
	/// <summary>
	/// The parameter type.
	/// </summary>
	public string Type;

	/// <summary>
	/// The name of the parameter.
	/// </summary>
	public string Name;

	/// <summary>
	/// True if it's an optional parameter.
	/// </summary>
	public bool IsOptional;

	/// <summary>
	/// Default value as a raw source code token (if it's a string, you must encapsulate it in " for example).
	/// </summary>
	public string DefaultValue;

	/// <summary>
	/// Writes this parameter as ts source.
	/// </summary>
	/// <param name="builder"></param>
	public void ToSource(StringBuilder builder)
	{
		builder.Append(Name);

		if (IsOptional)
		{
			builder.Append('?');
		}

		builder.Append(':');
		builder.Append(Type);

		if (!string.IsNullOrEmpty(DefaultValue))
		{
			builder.Append('=');
			builder.Append(DefaultValue);
		}
	}
}

/// <summary>
/// A method in a class.
/// </summary>
public class ApiMethod
{
	/// <summary>
	/// Method visibility.
	/// </summary>
	public string Visibility = "public";

	/// <summary>
	/// The return type of the method.
	/// </summary>
	public string ReturnType;

	/// <summary>
	/// the parameters of the method.
	/// </summary>
	public List<ApiMethodParameter> Parameters;

	/// <summary>
	/// Raw function body.
	/// </summary>
	public string Body;

	/// <summary>
	/// Method name.
	/// </summary>
	public string Name;

	/// <summary>
	/// Writes this method as ts source.
	/// </summary>
	/// <param name="builder"></param>
	public void ToSource(StringBuilder builder)
	{
		builder.Append('\t');
		builder.Append(Name);

		builder.Append('(');

		if (Parameters != null)
		{
			for (var i = 0; i < Parameters.Count;i++)
			{
				if (i != 0)
				{
					builder.Append(',');
				}

				var parameter = Parameters[i];
				parameter.ToSource(builder);
			}
		}

		builder.Append(')');

		if (!string.IsNullOrEmpty(ReturnType))
		{
			builder.Append(':');
			builder.Append(ReturnType);
		}

		builder.Append("{\n\t\t");
		builder.Append(Body);
		builder.Append("\n\t}\n");
	}

	private ApiMethodParameter CreateParameter(string name, string type)
	{
		if (Parameters == null)
		{
			Parameters = new List<ApiMethodParameter>();
		}
		var parameter = new ApiMethodParameter() { Name = name, Type = type };
		return parameter;
	}

	/// <summary>
	/// Inserts a new param for this method at the given parameter index. Use AddParameter to insert at the current end of the list.
	/// </summary>
	/// <param name="index"></param>
	/// <param name="name"></param>
	/// <param name="type"></param>
	/// <returns></returns>
	public ApiMethodParameter InsertParameter(int index, string name, string type)
	{
		var parameter = CreateParameter(name, type);
		Parameters.Insert(index, parameter);
		return parameter;
	}

	/// <summary>
	/// Creates a new param for this method.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="type"></param>
	/// <returns></returns>
	public ApiMethodParameter AddParameter(string name, string type)
	{
		var parameter = CreateParameter(name, type);
		Parameters.Add(parameter);
		return parameter;
	}
}

/// <summary>
/// An enum.
/// </summary>
public class ApiEnum : ApiType
{
	/// <summary>
	/// Creates a new enum with the given namee.
	/// </summary>
	/// <param name="name"></param>
	public ApiEnum(string name) : base(name)
	{
	}

	/// <summary>
	/// Writes this class as typescript source.
	/// </summary>
	/// <param name="builder"></param>
	public override void ToSource(StringBuilder builder)
	{
		builder.Append("export enum ");
		builder.Append(Name);

		builder.Append("{\n");

		foreach (var field in Fields)
		{
			if (field == null)
			{
				continue;
			}

			field.ToEnumSource(builder);
		}

		builder.Append("}\n");
	}

}

/// <summary>
/// A class with functions.
/// </summary>
public class ApiClass : ApiType
{
	/// <summary>
	/// Adds "export const ExportInstanceAs = new X();" after the class.
	/// </summary>
	public string ExportInstanceAs;

	/// <summary>
	/// The functions in this class.
	/// </summary>
	public List<ApiMethod> Methods;

	/// <summary>
	/// The properties in this class.
	/// </summary>
	public List<ApiProperty> Properties;

	/// <summary>
	/// Creates a new class with the given name/ base type.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="baseType"></param>
	public ApiClass(string name, string baseType = null) : base(name, baseType)
	{
	}

	/// <summary>
	/// Adds a get .. {} property.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public ApiProperty AddGetProperty(string name)
	{
		if (Properties == null)
		{
			Properties = new List<ApiProperty>();
		}

		var property = new ApiProperty() { Name = name };
		Properties.Add(property);
		return property;
	}

	/// <summary>
	/// Starts a new method on this class.
	/// </summary>
	/// <param name="name"></param>
	public ApiMethod AddMethod(string name)
	{
		if (Methods == null)
		{
			Methods = new List<ApiMethod>();
		}

		// Does a method with this name exist already?
		if (Methods.Find(mtd => mtd.Name == name) != null)
		{
			throw new Exception("Controller methods must have unique names for the typescript bindings to resolve them correctly. " +
				"A method called '" + name + "' was added at least twice to '" + Name + "'. " +
				"Note that this also means you cannot stack multiple [HttpGet], [HttpPost] etc on one method.");
		}

		var method = new ApiMethod() { Name = name };
		Methods.Add(method);
		return method;
	}

	/// <summary>
	/// Writes the methods of the class to the given builder.
	/// </summary>
	/// <param name="sb"></param>
	public void WriteMethods(StringBuilder sb)
	{
		if (Methods == null)
		{
			return;
		}

		for (var i = 0; i < Methods.Count; i++)
		{
			var method = Methods[i];
			method.ToSource(sb);
		}
	}

	/// <summary>
	/// Writes the properties of the class to the given builder.
	/// </summary>
	/// <param name="sb"></param>
	public void WriteProperties(StringBuilder sb)
	{
		if (Properties == null)
		{
			return;
		}

		for (var i = 0; i < Properties.Count; i++)
		{
			var properties = Properties[i];
			properties.ToSource(sb);
		}
	}

	/// <summary>
	/// Writes this class as typescript source.
	/// </summary>
	/// <param name="builder"></param>
	public override void ToSource(StringBuilder builder)
	{
		builder.Append("export class ");
		builder.Append(Name);
		WriteGenerics(builder);

		if (!string.IsNullOrEmpty(BaseType))
		{
			builder.Append(" extends ");
			builder.Append(BaseType);
		}

		builder.Append("{\n");
		WriteFields(builder);
		WriteMethods(builder);
		WriteProperties(builder);
		builder.Append("}\n");

		if (!string.IsNullOrEmpty(ExportInstanceAs))
		{
			builder.Append("const ");
			builder.Append(ExportInstanceAs);
			builder.Append(" = new ");
			builder.Append(Name);
			builder.Append("();\nexport { ");
			builder.Append(ExportInstanceAs);
			builder.Append(" }\n");
		}

	}

}

/// <summary>
/// A property on the class.
/// </summary>
public class ApiProperty
{
	/// <summary>
	/// The name of the property.
	/// </summary>
	public string Name;

	/// <summary>
	/// The source content of the property.
	/// </summary>
	public string Body;


	/// <summary>
	/// Writes this method as ts source.
	/// </summary>
	/// <param name="builder"></param>
	public void ToSource(StringBuilder builder)
	{
		builder.Append("\tget ");
		builder.Append(TypeScriptService.LcFirst(Name));

		builder.Append("(){\n\t\t");
		builder.Append(Body);
		builder.Append("\n\t}\n");
	}

}

/// <summary>
/// A simple type description for typescript.
/// </summary>
public class ApiType
{
	/// <summary>
	/// The name of the type.
	/// </summary>
	public string Name;

	/// <summary>
	/// The base type. Can be generic.
	/// </summary>
	public string BaseType;

	/// <summary>
	/// The set of fields on this type.
	/// </summary>
	public List<ApiTypeField> Fields = [];

	/// <summary>
	/// Set if this type is a generic type definition.
	/// </summary>
	public List<string> GenericNames;

	/// <summary>
	/// The namespace the type is in.
	/// </summary>
	public ApiNamespace Namespace;

	/// <summary>
	/// Create type by name
	/// </summary>
	/// <param name="name"></param>
	/// <param name="baseType"></param>
	public ApiType(string name, string baseType = null)
	{
		Name = name;
		BaseType = baseType;
	}

	/// <summary>
	/// Add a field (chainable)
	/// </summary>
	/// <param name="name"></param>
	/// <param name="type"></param>
	/// <param name="nullable"></param>
	/// <param name="defaultValueSrc"></param>
	/// <param name="visibility"></param>
	public ApiTypeField AddField(string name, string type, bool nullable = false, string defaultValueSrc = null, string visibility = null)
	{
		var field = new ApiTypeField()
		{
			Name = name,
			Type = type,
			Nullable = nullable,
			DefaultValueSource = defaultValueSrc,
			Visibility = visibility
		};
		Fields.Add(field);
		return field;
	}

	/// <summary>
	/// Writes the fields as ts source.
	/// </summary>
	/// <param name="builder"></param>
	public void WriteFields(StringBuilder builder)
	{
		foreach (var field in Fields)
		{
			if (field == null)
			{
				continue;
			}

			field.ToSource(builder);
		}
	}

	/// <summary>
	/// Writes the generic names as ts source. Writes nothing if there are none.
	/// </summary>
	/// <param name="builder"></param>
	public void WriteGenerics(StringBuilder builder)
	{
		if (GenericNames != null && GenericNames.Count > 0)
		{
			builder.Append("<");

			for (var i = 0; i < GenericNames.Count; i++)
			{
				if (i != 0)
				{
					builder.Append(", ");
				}

				builder.Append(GenericNames[i]);
			}

			builder.Append(">");
		}
	}

	/// <summary>
	/// Writes this type as ts source to the given builder.
	/// </summary>
	/// <param name="builder"></param>
	public virtual void ToSource(StringBuilder builder)
	{
		builder.Append("export interface ");
		builder.Append(Name);
		WriteGenerics(builder);

		if (!string.IsNullOrEmpty(BaseType))
		{
			builder.Append(" extends ");
			builder.Append(BaseType);
		}

		builder.Append("{\n");
		WriteFields(builder);
		builder.Append("};\n");
	}
}

/// <summary>
/// A field on a type.
/// </summary>
public class ApiTypeField
{
	/// <summary>
	/// The name of the field. The first letter is always lowercased except for enums so this can just be as-is from C#.
	/// </summary>
	public string Name;
	/// <summary>
	/// The type (can be an array etc).
	/// </summary>
	public string Type;
	/// <summary>
	/// True if this field can be null (|null will be appended to the type).
	/// </summary>
	public bool Nullable;
	/// <summary>
	/// True if this field is optional (uses ?).
	/// </summary>
	public bool Optional;
	/// <summary>
	/// An optional default value as typescript source.
	/// </summary>
	public string DefaultValueSource;
	/// <summary>
	/// An optional visibility (public/private).
	/// </summary>
	public string Visibility;

	/// <summary>
	/// Writes this field as ts source.
	/// </summary>
	/// <param name="builder"></param>
	public void ToSource(StringBuilder builder)
	{
		if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Type))
		{
			return;
		}

		builder.Append('\t');

		if (Visibility != null)
		{
			builder.Append(Visibility);
			builder.Append(' ');
		}

		builder.Append(char.ToLower(Name[0]));
		builder.Append(Name.Substring(1));

		if (Optional)
		{
			builder.Append("?");
		}

		builder.Append(':');
		builder.Append(Type);

		if (Nullable)
		{
			builder.Append("|null");
		}

		if (!string.IsNullOrEmpty(DefaultValueSource))
		{
			builder.Append("=");
			builder.Append(DefaultValueSource);
		}

		builder.Append(";\n");
	}

	/// <summary>
	/// Writes this field as ts source for an enum field.
	/// </summary>
	/// <param name="builder"></param>
	public void ToEnumSource(StringBuilder builder)
	{
		if (string.IsNullOrEmpty(Name))
		{
			return;
		}

		builder.Append('\t');
		builder.Append(Name);

		if (!string.IsNullOrEmpty(DefaultValueSource))
		{
			builder.Append("=");
			builder.Append(DefaultValueSource);
		}

		builder.Append(",\n");
	}
}

/// <summary>
/// An import statement.
/// </summary>
public class ApiImport
{
	/// <summary>
	/// The alias for the file being imported from. Always starts with Api/.
	/// </summary>
	public string FromAlias;

	/// <summary>
	/// The types being imported.
	/// </summary>
	public List<string> Types = [];

	/// <summary>
	/// Null if not importing the default.
	/// </summary>
	public string NamedDefault;

	/// <summary>
	/// Creates a new importer for the given alias.
	/// </summary>
	/// <param name="fromAlias"></param>
	/// <exception cref="Exception"></exception>
	public ApiImport(string fromAlias)
	{
		if (fromAlias == null)
		{
			throw new Exception("Invalid import alias (required)");
		}

		FromAlias = fromAlias;
	}
}