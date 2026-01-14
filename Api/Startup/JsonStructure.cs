using Api.AutoForms;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Translate;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace Api.Startup
{

	/// <summary>
	/// Describes the available fields on a particular type.
	/// This exists so we can, for example, role restrict setting particular fields.
	/// </summary>
	public class JsonStructure {
		/// <summary>
		/// The role that this structure is for.
		/// </summary>
		public Role ForRole;

		/// <summary>
		/// All fields in this structure as typeless JsonField refs.
		/// </summary>
		public virtual IEnumerable<KeyValuePair<string, JsonField>> AllFields
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Gets a meta field. Common names are "title" and "description".
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public virtual JsonField GetMetaField(string name)
		{
			return null;
		}
		
		/// <summary>
		/// The meta fields in this type. The keys are always lowercase. For example "title" and "description".
		/// These are set by applying [Meta("fieldname")] to your content type's fields. 
		/// Note that title and description will always exist, unless a content type does not have any fields at all.
		/// </summary>
		public virtual IEnumerable<KeyValuePair<string, JsonField>> AllMetaFields
		{
			get
			{
				throw new NotImplementedException();
			}
		}

	}

	/// <summary>
	/// Describes the available fields on a particular type.
	/// This exists so we can, for example, role restrict setting particular fields.
	/// </summary>
	public class JsonStructure<T, ID> : JsonStructure
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// The host service.
		/// </summary>
		public AutoService<T, ID> Service;

		/// <summary>
		/// Type reader writer for this structure.
		/// </summary>
		public TypeReaderWriter<T> TypeIO;

		/// <summary>
		/// All raw fields in this structure.
		/// </summary>
		public Dictionary<string, JsonField<T, ID>> Fields;
		/// <summary>
		/// All meta fields in this structure. Common ones are e.g. "title" and "description".
		/// </summary>
		public Dictionary<string, JsonField<T, ID>> MetaFields;

		
		/// <summary>
		/// Creates a new structure for the given role.
		/// </summary>
		/// <param name="forRole"></param>
		public JsonStructure(Role forRole)
		{
			ForRole = forRole;
			Fields = new Dictionary<string, JsonField<T, ID>>();
			MetaFields = new Dictionary<string, JsonField<T, ID>>();
		}

		/// <summary>
		/// Counts the number of include strings in the given include line.
		/// </summary>
		/// <param name="includes"></param>
		/// <returns></returns>
		public int CountIncludes(string includes)
		{
			if (includes == null)
			{
				return 0;
			}

			var chars = includes.AsSpan();

			var hasSomething = false;
			var count = 0;

			for (var i = 0; i < chars.Length; i++)
			{
				if (chars[i] == ',')
				{
					if (hasSomething)
					{
						count++;
						hasSomething = false;
					}
				}
				else
				{
					hasSomething = true;
				}
			}

			if (hasSomething)
			{
				count++;
			}

			return count;
		}

		/// <summary>
		/// All fields in this structure as typeless JsonField refs.
		/// </summary>
		public override IEnumerable<KeyValuePair<string, JsonField>> AllFields
		{
			get
			{
				foreach (var fieldKvp in Fields)
				{
					yield return new KeyValuePair<string, JsonField>(fieldKvp.Key, fieldKvp.Value);
				}
			}
		}
		
		/// <summary>
		/// All meta fields in this structure as typeless JsonField refs.
		/// </summary>
		public override IEnumerable<KeyValuePair<string, JsonField>> AllMetaFields
		{
			get
			{
				foreach (var fieldKvp in MetaFields)
				{
					yield return new KeyValuePair<string, JsonField>(fieldKvp.Key, fieldKvp.Value);
				}
			}
		}

		/// <summary>
		/// Builds this structure now. It looks at all public fields and properties of a type
		/// and for each one, triggers an event. The event can return either nothing at all - which will outright block the field - 
		/// or the event can add a special value handler which will map the raw JSON value to the actual object for us.
		/// </summary>
		public async ValueTask Build(ContentFields fields, Api.Eventing.EventHandler<JsonField<T, ID>> beforeSettable, Api.Eventing.EventHandler<JsonField<T, ID>> beforeGettable)
		{
			var context = new Context();

			// Load the access rules:
			var fieldAccessService = Services.Get<ContentFieldAccessRuleService>();

			var accessRules = await fieldAccessService.Where("RoleId=? and EntityName=?", DataOptions.IgnorePermissions)
				.Bind(ForRole.Id)
				.Bind(typeof(T).Name)
				.ListAll(context);

			// Get the perm attributes on the type, if any:
			var permAttributes = typeof(T).GetCustomAttributes<PermissionsAttribute>();

			foreach (var contentField in fields.List)
			{
				JsonField<T, ID> jsonField;

				if (contentField.FieldInfo != null)
				{
					var field = contentField.FieldInfo;

					jsonField = new JsonField<T, ID>()
					{
						Name = field.Name,
						OriginalName = field.Name,
						Attributes = MergeAttributes(permAttributes, contentField.Attributes),
						Structure = this,
						TargetType = field.FieldType,
						FieldInfo = field,
						ContentField = contentField
					};

				}
				else
				{
					var property = contentField.PropertyInfo;

					jsonField = new JsonField<T, ID>()
					{
						Name = property.Name,
						OriginalName = property.Name,
						Attributes = MergeAttributes(permAttributes, contentField.Attributes),
						Structure = this,
						PropertyInfo = property,
						TargetType = property.PropertyType,
						PropertyGet = property.GetGetMethod(),
						PropertySet = property.GetSetMethod(),
						ContentField = contentField,
						Writeable = property.CanWrite,
						// Default behaviour is to hide (from autoforms) non-writeable properties.
						// Using BeforeSettable and setting Hide to false will display a readonly field if you want it to be visible.
						Hide = !property.CanWrite
					};
				}

				// Locate an access rule if there is one:
				var fieldName = jsonField.Name;
				var lowercaseFirst = char.ToLower(fieldName[0]) + fieldName.Substring(1);
				var matchedAccessRule = accessRules == null ? null : accessRules.Find(rule => rule.FieldName == lowercaseFirst);
				jsonField.AccessRule = matchedAccessRule;

				await TryAddField(context, jsonField, beforeSettable, beforeGettable);
			}

			// Add local list virtual fields:
			foreach (var field in fields.VirtualList)
			{
				if (field.VirtualInfo == null || field.VirtualInfo.IsList == false)
				{
					continue;
				}

				var isExplicit = field.VirtualInfo.IsExplicit;

				if (isExplicit)
				{
					// This field exists, but should only appear if it is explicitly asked for.
					// This is important for includes and also e.g. autoform.
					// First though, we need to identify if we're one of the exlusions.
					if (field.VirtualInfo.IsImplicitFor(typeof(T)))
					{
						isExplicit = false;
					}
				}

				var jsonField = new JsonField<T, ID>()
				{
					Name = field.VirtualInfo.FieldName,
					OriginalName = field.VirtualInfo.FieldName,
					Structure = this,
					Hide = isExplicit,
					ContentField = field,
					IsExplicit = isExplicit,
					Attributes = MergeAttributes(permAttributes, field.Attributes),
					AfterId = true
				};

				if (field.VirtualInfo.ValueGeneratorType != null)
				{
					// Instance the value generator now.
					var typeToInstance = field.VirtualInfo.ValueGeneratorType.MakeGenericType(typeof(T), typeof(ID));
					var valueGenerator = Activator.CreateInstance(typeToInstance) as VirtualFieldValueGenerator<T, ID>;

					if (valueGenerator == null)
					{
						throw new Exception(
							"A bad ValueGeneratorType has been specified. Must inherit from VirtualFieldValueGenerator<T, ID>. The type that failed was " + 
							field.VirtualInfo.ValueGeneratorType.Name
						);
					}

					jsonField.FieldValueGenerator = valueGenerator;

					// These fields are exclusively readonly.
					jsonField.Writeable = false;
					jsonField.TargetType = valueGenerator.OutputType;
				}
				else
				{
					// Get the ID type of the field:
					var idType = field.VirtualInfo.Type.GetField("Id").FieldType;
					jsonField.TargetType = typeof(IEnumerable<>).MakeGenericType(idType);
					jsonField.Data["contentType"] = field.VirtualInfo.Type.Name;
				}
				
				// Note: initial module is set by TryAdd.
				await TryAddField(context, jsonField, beforeSettable, beforeGettable);
			}

			// Add global virtual fields:
			foreach (var kvp in ContentFields.GlobalVirtualFields)
			{
				var field = kvp.Value;

				var isExplicit = field.VirtualInfo.IsExplicit;

				if (isExplicit)
				{
					// This field exists, but should only appear if it is explicitly asked for.
					// This is important for includes and also e.g. autoform.
					// First though, we need to identify if we're one of the exlusions.
					if (field.VirtualInfo.IsImplicitFor(typeof(T)))
					{
						isExplicit = false;
					}
				}

				var jsonField = new JsonField<T, ID>()
				{
					Name = field.VirtualInfo.FieldName,
					OriginalName = field.VirtualInfo.FieldName,
					Structure = this,
					Hide = isExplicit,
					ContentField = field,
					IsExplicit = isExplicit,
					Attributes = MergeAttributes(permAttributes, field.Attributes),
					AfterId = true
				};

				if (field.VirtualInfo.ValueGeneratorType != null)
				{
					// Instance the value generator now.
					var typeToInstance = field.VirtualInfo.ValueGeneratorType.MakeGenericType(typeof(T), typeof(ID));
					var valueGenerator = Activator.CreateInstance(typeToInstance) as VirtualFieldValueGenerator<T, ID>;

					if (valueGenerator == null)
					{
						throw new Exception(
							"A bad ValueGeneratorType has been specified. Must inherit from VirtualFieldValueGenerator<T, ID>. The type that failed was " +
							field.VirtualInfo.ValueGeneratorType.Name
						);
					}

					jsonField.FieldValueGenerator = valueGenerator;

					// These fields are exclusively readonly.
					jsonField.Writeable = false;
					jsonField.TargetType = valueGenerator.OutputType;
				}
				else
				{
					// Get the ID type of the field:
					var idType = field.VirtualInfo.Type.GetField("Id").FieldType;
					jsonField.TargetType = typeof(IEnumerable<>).MakeGenericType(idType);
					jsonField.Data["contentType"] = field.VirtualInfo.Type.Name;
				}

				// Note: initial module is set by TryAdd.
				await TryAddField(context, jsonField, beforeSettable, beforeGettable);
			}


			foreach (var kvp in fields.MetaFieldMap)
			{
				// Get the JsonField for the given ContentField.
				if (Fields.TryGetValue(kvp.Value.Name.ToLower(), out JsonField<T, ID> jsonField))
				{
					MetaFields[kvp.Key] = jsonField;
				}
			}
		}

		/// <summary>
		/// Check if the given type is a numeric one.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		private static bool IsNumericType(Type type)
		{
			if (type.IsGenericType)
			{
				var def = type.GetGenericTypeDefinition();

				if (def == typeof(Localized<>))
				{
					return IsNumericType(type.GetGenericArguments()[0]);
				}
				else if (def == typeof(Nullable<>))
				{
					return IsNumericType(type.GetGenericArguments()[0]);
				}

				return false;
			}

			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Single:
					return true;
				default:
					return false;
			}
		}

		private IEnumerable<Attribute> MergeAttributes(IEnumerable<Attribute> a, IEnumerable<Attribute> b)
		{
			if (a == null && b == null)
			{
				return Array.Empty<Attribute>();
			}

			if (a == null)
			{
				// b as-is is fine. (A as-is should be avoided as it would end up being shared by the output).
				return b;
			}

			var result = new List<Attribute>();
			result.AddRange(a);

			if (a != null)
			{
				result.AddRange(a);
			}

			if (b != null)
			{
				result.AddRange(b);
			}

			return result;
		}

		/// <summary>
		/// Adds the given field to this structure.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="field"></param>
		/// <param name="beforeSettable"></param>
		/// <param name="beforeGettable"></param>
		private async ValueTask TryAddField(Context context, JsonField<T, ID> field, Api.Eventing.EventHandler<JsonField<T, ID>> beforeSettable, Api.Eventing.EventHandler<JsonField<T, ID>> beforeGettable)
		{
			// Set the default just before the field event:
			field.SetDefaultDisplayModule();

			// Set if it's numeric:
			field.IsNumericField = IsNumericType(field.TargetType);

			// Fields are readable by default:
			string readRule = "true";
			string writeRule = "true";

			if (field.Attributes != null)
			{
				foreach (var attrib in field.Attributes)
				{
					var perm = attrib as PermissionsAttribute;
					if (perm == null)
					{
						// Not an attribute we care about.
						continue;
					}

					// Can this attrib be applied to this role?
					if (!RoleInKeySet(field.ForRole, perm.Roles))
					{
						// This attribute is for other roles.
						continue;
					}

					// Attribute is active for this role.

					if (!string.IsNullOrEmpty(perm.ReadRule))
					{
						readRule = perm.ReadRule;
					}

					if (!string.IsNullOrEmpty(perm.WriteRule))
					{
						writeRule = perm.WriteRule;
					}
				}
			}

			field.WriteRule = writeRule;
			field.ReadRule = readRule;
			var gettableField = await beforeGettable.Dispatch(context, field);
			
			if(gettableField == null){
				field.ReadRule = "false";
			}

			field = await beforeSettable.Dispatch(context, field);

			if (field != null && field.Attributes != null)
			{
				foreach (var attrib in field.Attributes)
				{
					if (attrib is Newtonsoft.Json.JsonIgnoreAttribute)
					{
						// We'll ignore these too.
						field.Hide = true;
					}
				}
			}

			// If the set event didn't outright block the field..
			if (field == null)
			{
				return;
			}
			
			var lowerName = field.Name.ToLower();

			Fields[lowerName] = field;
		}

		/// <summary>
		/// True if the given non-null key set applies to the given role.
		/// For example "members" applies to exactly 1 role.
		/// Can be comma separated and contain two keywords: * and admins (all admin roles).
		/// Can also use ! to invert. "*,!member" meaning everything but the member role.
		/// Note that the last matching rule wins: "!member,*" will be true for member as the * test comes later.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="keySet"></param>
		/// <returns></returns>
		private bool RoleInKeySet(Role role, string keySet)
		{
			if (role == null)
			{
				return false;
			}

			if (string.IsNullOrEmpty(keySet))
			{
				// null/empty is equiv to "*"
				return true;
			}

			var applicable = false;
			var roles = keySet.Split(",");

			for (var i = 0; i < roles.Length; i++)
			{
				var current = roles[i].Trim();
				if (current == "")
				{
					continue;
				}

				var negated = current[0] == '!';

				if (negated)
				{
					current = current.Substring(1);
				}

				if (current == "*")
				{
					applicable = !negated;
				}
				else if (current == "admins")
				{
					if (role.CanViewAdmin)
					{
						applicable = !negated;
					}
				}
				else if(role.Key == current)
				{
					applicable = !negated;
				}
			}

			return applicable;
		}

		/// <summary>
		/// Attempts to get a given case insensitive field.
		/// </summary>
		/// <param name="name"></param>
		public JsonField<T, ID> GetField(string name)
		{
			JsonField<T, ID> result;
			Fields.TryGetValue(name.ToLower(), out result);
			return result;
		}

		/// <summary>
		/// Gets a meta field. Common names are "title" and "description".
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public override JsonField GetMetaField(string name)
		{
			MetaFields.TryGetValue(name.ToLower(), out JsonField<T, ID> result);
			return result;
		}

		/// <summary>
		/// Gets a meta field. Common names are "title" and "description".
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public JsonField<T, ID> GetTypedMetaField(string name)
		{
			MetaFields.TryGetValue(name.ToLower(), out JsonField<T, ID> result);
			return result;
		}

	}

	/// <summary>
	/// A field within a JsonStructure.
	/// </summary>
	public class JsonField
	{
		/// <summary>
		/// The name of the field.
		/// </summary>
		public string Name;
		/// <summary>
		/// The original name of the field.
		/// </summary>
		public string OriginalName;
		/// <summary>
		/// If this is a property, the underlying PropertyInfo. Null otherwise.
		/// </summary>
		public PropertyInfo PropertyInfo;
		/// <summary>
		/// If this is a field, the underlying FieldInfo. Null otherwise.
		/// </summary>
		public FieldInfo FieldInfo;
		/// <summary>
		/// True if this is an explicit field. It only appears in includes if it is explicitly asked for. It doesn't appear on AutoForm.
		/// Note that Hide is also set to true when this is true.
		/// </summary>
		public bool IsExplicit;
		/// <summary>
		/// The content field that this originated from. Can be a global virtual one.
		/// </summary>
		public ContentField ContentField;
		/// <summary>
		/// Set this to true if it should only be applied after an objects ID is known.
		/// </summary>
		public bool AfterId;
		/// <summary>
		/// If this is a Property, the get method. Null otherwise.
		/// </summary>
		public MethodInfo PropertyGet;
		/// <summary>
		/// If this is a Property, the set method. Null otherwise.
		/// </summary>
		public MethodInfo PropertySet;
		/// <summary>
		/// The field/ property value type.
		/// </summary>
		public Type TargetType;
		/// <summary>
		/// True if this is a numeric field (int, double etc).
		/// </summary>
		public bool IsNumericField;
		/// <summary>
		/// True if this field is readable by this role. 
		/// Can also set Readable for a general true/false rather than conditional.
		/// </summary>
		public string ReadRule = "true";
		/// <summary>
		/// The code specified rule definining if this field is writeable by this role. 
		/// Can also set Writeable for a general true/false rather than conditional.
		/// </summary>
		public string WriteRule = "true";
		/// <summary>
		/// The field or property attributes.
		/// </summary>
		public IEnumerable<Attribute> Attributes;
		
		/// <summary>
		/// True if this field is basic readable. Sets the ReadRule to either "true" or "false".
		/// </summary>
		public bool Readable {
			get {
				return ReadRule == "true";
			}
			set {
				ReadRule = value ? "true" : "false";
			}
		}

		/// <summary>
		/// True if this field is basic writeable. Sets the WriteRule to either "true" or "false".
		/// </summary>
		public bool Writeable
		{
			get
			{
				return WriteRule == "true";
			}
			set
			{
				WriteRule = value ? "true" : "false";
			}
		}

		/// <summary>
		/// The display module when this field is displayed in a form.
		/// Can be overriden durign field load.
		/// </summary>
		public string Module;
		/// <summary>
		/// The set of props to give to the display module when displaying this field in a form.
		/// Must be json serializable.
		/// </summary>
		public ConcurrentDictionary<string, object> Data = new ConcurrentDictionary<string, object>();
		/// <summary>
		/// True if this field should not appear in forms. Non-writeable properties are hidden by default.
		/// </summary>
		public bool Hide;

		/// <summary>
		/// Gets the read access rule as the raw textual filter, if there is one.
		/// </summary>
		/// <returns></returns>
		public virtual string GetReadAccessRuleText()
		{
			return null;
		}

		/// <summary>
		/// Sets up the default display module for common field types.
		/// This runs just before the field load event occurs.
		/// </summary>
		public void SetDefaultDisplayModule()
		{
			var isVirtualList = ContentField != null && ContentField.VirtualInfo != null;
			Module = isVirtualList ? "Admin/MultiSelect" : "UI/Input";
			var type = "text";
			var name = OriginalName;
			var labelName = name;
			var fieldType = TargetType;
			var isLocalized = false;

			if (fieldType.IsGenericType)
			{
				var genericFieldType = fieldType.GetGenericTypeDefinition();

				if (genericFieldType == typeof(Localized<>))
				{
					isLocalized = true;

					// Everything else acts as the interior type.
					fieldType = fieldType.GetGenericArguments()[0];
				}
			}

			var underlying = Nullable.GetUnderlyingType(fieldType);

			if (underlying != null)
			{
				fieldType = underlying;
			}

			if (isVirtualList)
			{
				var titleField = ContentField.VirtualInfo.MetaTitleField;
				if (titleField != null)
				{
					Data["field"] = titleField.Name;
				}
			}

			// If the field is a string and ends with Json, it's canvas:
			if (fieldType == typeof(string) && labelName.EndsWith("Json"))
			{
				type = "canvas";

				// Remove "Json" from the end of the label:
				labelName = labelName[0..^4];
	}
			else if (fieldType == typeof(string) && labelName.EndsWith("Ref"))
			{
				type = "image";

				// Remove "Ref" from the end of the label:
				labelName = labelName[0..^3];
			}
			else if (fieldType == typeof(string) && (labelName.EndsWith("Color") || labelName.EndsWith("Colour")))
			{
				type = "color";

				// Retain the word color/ colour in this one
			}
			/*
			else if (fieldType == typeof(DateTime))
			{
				type = "datetime-local";

				if(labelName.EndsWith("Utc")){
					// Remove "Utc" from the end of the label:
					labelName = labelName.Substring(0, labelName.Length - 3);
				}
			}
			*/
			
			else if ((fieldType == typeof(int) || fieldType == typeof(uint)) && labelName != "Id" && labelName.EndsWith("Id") && ContentTypes.GetType(labelName[0..^2].ToLower()) != null)
			{
				// Remove "Id" from the end of the label:
				labelName = labelName[0..^2];
				
				Data["contentType"] = labelName;
				Module = "Admin/ContentSelect";
			}
			else if (fieldType == typeof(bool))
			{
				type = "checkbox";
			}

			Data["label"] = SpaceCamelCase(labelName);
			Data["name"] = FirstCharacterToLower(name);
			Data["type"] = type;

			if (isLocalized)
			{
				Data["localized"] = true;
			}

			// Any of these [Module] or inheritors?
			foreach (var attrib in Attributes)
			{
				if (attrib is ModuleAttribute)
				{
					var module = attrib as ModuleAttribute;

					if (module.Name != null)
					{
						Module = module.Name;
					}
					
					if(module.Hide)
					{
						Hide = true;
					}
				}
				else if (attrib is DataAttribute)
				{
					var data = attrib as DataAttribute;
					Data[data.Name] = data.Value;
				}
			}
			
		}

		private readonly static Regex SplitCamelCaseRegex = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

		/// <summary>
		/// Adds spaces to a CamelCase string (so it becomes "Camel Case")
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static string SpaceCamelCase(string s)
		{
			return SplitCamelCaseRegex.Replace(s, " ");
		}

		/// <summary>
		/// Lowercases the first character of the given string.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string FirstCharacterToLower(string str)
		{
			if (String.IsNullOrEmpty(str) || Char.IsLower(str, 0))
				return str;

			return Char.ToLowerInvariant(str[0]) + str[1..];
		}

	}

	/// <summary>
	/// A field within a JsonStructure.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="ID"></typeparam>
	public class JsonField<T, ID> : JsonField
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// The access rule for this field.
		/// </summary>
		public ContentFieldAccessRule AccessRule;

		/// <summary>
		/// The structure this field belongs to.
		/// </summary>
		public JsonStructure<T, ID> Structure;
		/// <summary>
		/// If this field is an includable virtual field, it can potentially generate a value at runtime.
		/// This is the generator for the value if there is one.
		/// </summary>
		public VirtualFieldValueGenerator<T, ID> FieldValueGenerator;
		/// <summary>
		/// An event which is called when the value is set. It returns the value it wants to be set.
		/// </summary>
		public EventHandler<object, T, JToken> OnSetValue = new EventHandler<object, T, JToken>();

		private FieldGrantRule<T, ID> _readAccessRule;
		private FieldGrantRule<T, ID> _writeAccessRule;

		/// <summary>
		/// Gets the fields write access rule (filter text) if there is one set. Null/ empty string otherwise.
		/// </summary>
		/// <returns></returns>
		public string GetWriteAccessRuleText()
		{
			if (AccessRule == null)
			{
				return null;
			}

			return AccessRule.CanWrite;
		}

		/// <summary>
		/// Gets the write access filter.
		/// </summary>
		/// <returns></returns>
		public FieldGrantRule<T, ID> GetWriteAccessRule()
		{
			// This whole JsonField is reconstructed 
			// whenever the role is edited so we can safely just cache a constructed filter in here.
			var ar = _writeAccessRule;

			if (ar != null)
			{
				return ar;
			}

			var text = GetWriteAccessRuleText();
			ar = new FieldGrantRule<T, ID>(this, text, WriteRule);
			_writeAccessRule = ar;
			return ar;
		}

		/// <summary>
		/// Gets the fields read access rule (filter text) if there is one set. Null/ empty string otherwise.
		/// </summary>
		/// <returns></returns>
		public override string GetReadAccessRuleText()
		{
			if (AccessRule == null)
			{
				return null;
			}

			return AccessRule.CanRead;
		}

		/// <summary>
		/// Gets the read access filter.
		/// </summary>
		/// <returns></returns>
		public FieldGrantRule<T, ID> GetReadAccessRule()
		{
			// This whole JsonField is reconstructed 
			// whenever the role is edited so we can safely just cache a constructed filter in here.
			var ar = _readAccessRule;

			if (ar != null)
			{
				return ar;
			}

			var text = GetReadAccessRuleText();
			ar = new FieldGrantRule<T, ID>(this, text, ReadRule);
			_readAccessRule = ar;
			return ar;
		}

		/// <summary>
		/// The role that this is for.
		/// </summary>
		public Role ForRole
		{
			get
			{
				return Structure.ForRole;
			}
		}
		
		private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

		private static DateTime ConvertFromJsUnixTimestamp(double timestamp)
		{
			return _unixEpoch.AddSeconds(timestamp / 1000);
		}

		private async ValueTask<object> GetTargetValue(Context context, T onObject, JToken value)
		{
			object targetValue = value;

			if (OnSetValue != null)
			{
				targetValue = await OnSetValue.Dispatch(context, targetValue, onObject, value);
			}

			// Do this rather than use value just in case the OnSetValue returned a JToken:
			if (targetValue is JToken targetJToken)
			{
				// Still a JToken - lets try and map it through now.
				targetValue = ConvertToken(targetJToken, TargetType);
			}

			return targetValue;
		}

		private object ConvertToken(JToken src, Type targetType)
		{
			if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Localized<>))
			{
				var argType = targetType.GetGenericArguments()[0];
				var baseVal = ConvertToken(src, argType);
				return Activator.CreateInstance(targetType, baseVal);
			}

			if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
			{
				// Special case for a date. If the value is numeric, it's a JS compatible timestamp (unix timestamp in *milliseconds*).
				// Otherwise, it's the JS compatible date string.

				if (src.Type == JTokenType.Integer || src.Type == JTokenType.Float)
				{
					// JS Timestamp (milliseconds).
					var msTimestamp = src.ToObject<double>();

					return ConvertFromJsUnixTimestamp(msTimestamp);
				}

				var str = src.ToObject<string>();

				if (string.IsNullOrWhiteSpace(str))
				{
					if (targetType == typeof(DateTime))
					{
						throw new PublicException("A date is required for " + Name, "date_format");
					}

					return null;
				}
				else if (ulong.TryParse(str, out ulong timestamp))
				{
					// it's a timestamp, in milliseconds from year 0.
					// It was parsed as a ulong because negative numbers are not permitted here (BC years).
					// Milliseconds from 0 to ticks is simply a multiply:
					return new DateTime((long)(timestamp * 10000));
				}
				else if (DateTime.TryParse(
					str,
					CultureInfo.InvariantCulture,
					System.Globalization.DateTimeStyles.RoundtripKind,
					out DateTime dateResult))
				{
					return dateResult;
				}

				// Unrecognised date format.
				throw new PublicException("Unrecognised date format", "date_format");
			}

			if (targetType == typeof(JsonString))
			{
				var str = src.Type == JTokenType.Null ? null : src.Value<string>();
				return new JsonString(str);
			}

			if (src.Type == JTokenType.Null)
			{
				// Use the default targetValue:
				if (targetType.IsValueType)
				{
					return Activator.CreateInstance(targetType);
				}
				
				return null;
			}
			
			if (src.Type == JTokenType.String && IsNumericField)
			{
				// can be e.g. an empty string on a numeric field.
				var str = src.Value<string>();

				if (string.IsNullOrEmpty(str))
				{
					// Use the default value:
					return Activator.CreateInstance(targetType);
				}
				
				// Try parse:
				return ConvertToObjectNumber(str, targetType);
			}

			return src.ToObject(targetType);
		}

		/// <summary>
		/// Allocating number parse to the target value.
		/// </summary>
		/// <param name="srcValue"></param>
		/// <param name="targetType"></param>
		/// <returns></returns>
		private object ConvertToObjectNumber(string srcValue, Type targetType)
		{
			var underlying = Nullable.GetUnderlyingType(targetType);
			var isNullable = false;

			if (underlying != null)
			{
				targetType = underlying;
				isNullable = true;
			}

			switch (Type.GetTypeCode(targetType))
			{
				case TypeCode.Byte:

					if (!byte.TryParse(srcValue, out byte b))
					{
						return isNullable ? null : (byte)0;
					}

					return isNullable ? (byte?)b : b;

				case TypeCode.SByte:

					if (!sbyte.TryParse(srcValue, out sbyte sb))
					{
						return isNullable ? null : (sbyte)0;
					}

					return isNullable ? (sbyte?)sb : sb;

				case TypeCode.UInt16:

					if (!ushort.TryParse(srcValue, out ushort us))
					{
						return isNullable ? null : (ushort)0;
					}

					return isNullable ? (ushort?)us : us;

				case TypeCode.UInt32:

					if (!uint.TryParse(srcValue, out uint ui))
					{
						return isNullable ? null : (uint)0;
					}

					return isNullable ? (uint?)ui : ui;

				case TypeCode.UInt64:

					if (!ulong.TryParse(srcValue, out ulong ul))
					{
						return isNullable ? null : (ulong)0;
					}

					return isNullable ? (ulong?)ul : ul;

				case TypeCode.Int16:

					if (!short.TryParse(srcValue, out short s))
					{
						return isNullable ? null : (short)0;
					}

					return isNullable ? (short?)s : s;

				case TypeCode.Int32:

					if (!int.TryParse(srcValue, out int i))
					{
						return isNullable ? null : (int)0;
					}

					return isNullable ? (int?)i : i;

				case TypeCode.Int64:

					if (!long.TryParse(srcValue, out long l))
					{
						return isNullable ? null : (long)0;
					}

					return isNullable ? (long?)l : l;

				case TypeCode.Decimal:

					if (!decimal.TryParse(srcValue, out decimal de))
					{
						return isNullable ? null : (decimal)0;
					}

					return isNullable ? (decimal?)de : de;

				case TypeCode.Double:

					if (!double.TryParse(srcValue, out double d))
					{
						return isNullable ? null : 0d;
					}

					return isNullable ? (double?)d : d;

				case TypeCode.Single:

					if (!float.TryParse(srcValue, out float f))
					{
						return isNullable ? null : 0f;
					}

					return isNullable ? (float?)f : f;
					
				default:
					return null;
			}
		}

		/// <summary>
		/// Sets the field value from a textual string. If the field type is numeric, the number will be parsed from the string or 0.
		/// Currently uses reflection so will result in allocations when setting value types.
		/// </summary>
		/// <param name="onObject"></param>
		/// <param name="srcValue"></param>
		public void SetFieldValue(T onObject, string srcValue)
		{
			object valueToSet = null;

			if (IsNumericField)
			{
				// byte, int etc. Can be nullable.
				valueToSet = ConvertToObjectNumber(srcValue, TargetType);
			}
			else if (TargetType == typeof(bool?))
			{
				if (srcValue == null)
				{
					valueToSet = (bool?)null;
				}
				else if (srcValue == "1" || srcValue == "TRUE" || srcValue == "true" || srcValue == "True" || srcValue == "on" || srcValue == "On" || srcValue == "ON")
				{
					valueToSet = (bool?)true;
				}
				else
				{
					valueToSet = (bool?)false;
				}
			}
			else if (TargetType == typeof(bool))
			{
				if (srcValue == "1" || srcValue == "TRUE" || srcValue == "true" || srcValue == "True" || srcValue == "on" || srcValue == "On" || srcValue == "ON")
				{
					valueToSet = true;
				}
				else
				{
					valueToSet = false;
				}
			}
			else if (TargetType == typeof(Localized<string>))
			{
				// Currently the only Localized case actually used by the pot file bits which use this method.
				// Ideally needs conversion towards being a set of generated methods
				// but that is a much bigger edit on this rarely used method than..
				valueToSet = new Localized<string>(srcValue);
			}
			else if (TargetType == typeof(string))
			{
				valueToSet = srcValue;
			}
			else if (TargetType == typeof(DateTime?))
			{
				// Source string could be either numeric or ISO formatted.
				if (srcValue == null)
				{
					valueToSet = (DateTime?)null;
				}
				else if (double.TryParse(srcValue, out double ticks))
				{
					// It was a number of ticks
					valueToSet = (DateTime?)ConvertFromJsUnixTimestamp(ticks);
				}
				else
				{
					// ISO formatted date string
					if (DateTime.TryParse(srcValue, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dateResult))
					{
						valueToSet = dateResult;
					}
					else
					{
						// Do nothing
						return;
					}
				}
			}
			else if (TargetType == typeof(DateTime))
			{
				if (srcValue == null)
				{
					return;
				}

				if (double.TryParse(srcValue, out double ticks))
				{
					// It was a number of ticks
					valueToSet = (DateTime?)ConvertFromJsUnixTimestamp(ticks);
				}
				else
				{
					// ISO formatted date string
					if (DateTime.TryParse(srcValue, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dateResult))
					{
						valueToSet = dateResult;
					}
					else
					{
						// Do nothing
						return;
					}
				}
			}
			else
			{
				throw new Exception(
					"Can't set this field which is of type '" + TargetType.Name + "' from a string yet. " +
					"If it's a localized field specifically, then making this method broader is currently a todo."
				);
			}

			if (PropertySet != null)
			{
				PropertySet.Invoke(onObject, new object[] { valueToSet });
			}
			else if (FieldInfo != null)
			{
				FieldInfo.SetValue(onObject, valueToSet);
			}
		}
		
		/// <summary>
		/// Sets the given value on the field but only if it changed.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="onObject"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public async ValueTask SetFieldValue(Context context, T onObject, JToken value)
		{
			if (OnSetValue == null && Hide)
			{
				// Ignore these fields - they can only be set if they have an OnSetValue handler.
				return;
			}

			var targetValue = await GetTargetValue(context, onObject, value);

			// Note that both the setter and the FieldInfo can be null (readonly properties).
			if (PropertySet != null)
			{
				PropertySet.Invoke(onObject, new object[] { targetValue });
			}
			else if (FieldInfo != null)
			{
				FieldInfo.SetValue(onObject, targetValue);
			}
			else if (ContentField.VirtualInfo != null)
			{
				// It's a virtual list field. TargetValue is an IEnumerable<TARGET_ID_TYPE>.
				// Force uint for now:
				var idSet = targetValue as IEnumerable<uint>;
				var targetService = ContentField.VirtualInfo.Service;
				
				if (idSet != null && targetService != null)
				{
					// Create mappings from onObject -> entry.Id
					// * We have write access to the "source" object because we're in SetIfChanged.
					// * We have read access to the "target" because we just got it successfully.

					var idList = new List<ulong>();
					foreach (var id in idSet)
					{
						idList.Add(id);
					}

					onObject.Mappings.Set(ContentField.VirtualInfo.FieldName, idList);
				}
			}
		}

	}

	/// <summary>
	/// A shared object which represents an included field.
	/// </summary>
	public class IncludedField
	{
		/// <summary>
		/// The "Depth" of this included field. It's essentially the number of dots + 1. E.g "Tags.CreatorUser" has a depth of 2.
		/// </summary>
		public int Depth = 1;

		/// <summary>
		/// The structure it's from.
		/// </summary>
		public JsonStructure Structure;
	}

}