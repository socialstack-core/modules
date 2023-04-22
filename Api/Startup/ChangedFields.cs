
using Api.Database;
using Api.Translate;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Api.Startup {
	
	/// <summary>
	/// Tracks fields which have changed, without allocating anything. 
	/// Note that this is field specific (because only fields are persistent). Properties do not work.
	/// Limited to 64 fields on an object.
	/// </summary>
	public struct ChangedFields {
		
		/// <summary>
		/// Specific map of fields.
		/// </summary>
		public ChangedFields(ulong map){
		}

	}
	
	/// <summary>
	/// A map of field name -> field on a Content type. 
	/// This level does not consider e.g. role accessibility - it is just a raw, complete set of fields and properties, including virtual ones.
	/// </summary>
	public class ContentFields
	{

		/// <summary>
		///  Common field names used by entities which are used as a title when no [Meta("title")] is declared.
		/// </summary>
		private readonly static string[] CommonTitleNames = new string[] { "fullname", "username", "firstname", "title", "name", "url" }; // Title itself isn't first as some user tables have "title" (as in Mr/s etc).

		/// <summary>
		///  Common field names used by entities which are used as a description when no [Meta("description")] is declared.
		/// </summary>
		private readonly static string[] CommonDescriptionNames = new string[] { "description", "shortdescription", "bio", "biography", "about" };

		/// <summary>
		/// Global virtual fields. ListAs appears in here.
		/// </summary>
		private static Dictionary<string, ContentField> _globalVirtualFields;

		/// <summary>
		/// Global virtual fields. ListAs appears in here.
		/// </summary>
		public static Dictionary<string, ContentField> GlobalVirtualFields {
			get
			{
				if (_globalVirtualFields == null)
				{
					SetupGlobalFields();
				}

				return _globalVirtualFields;
			}
		}

		/// <summary>
		/// Inclusion sets that have been pre-generated, rooted from this set.
		/// </summary>
		public ConcurrentDictionary<string, IncludeSet> includeSets = new ConcurrentDictionary<string, IncludeSet>();

		/// <summary>
		/// The AutoService that this map is for.
		/// </summary>
		public AutoService Service;

		/// <summary>
		/// The type that this map is for.
		/// </summary>
		public Type InstanceType;

		/// <summary>
		/// Collects all include value generators. They are classes which inherit VirtualFieldValueGenerator.
		/// </summary>
		private static void SetupGlobalFields()
		{
			// Instance array:
			_globalVirtualFields = new Dictionary<string, ContentField>();

			// Collect all value generators:
			var allTypes = typeof(ChangedFields).Assembly.DefinedTypes;

			foreach (var typeInfo in allTypes)
			{
				// If it:
				// - Is a class
				// - Inherits VirtualFieldValueGenerator
				// Then we instance it.

				if (!typeInfo.IsClass)
				{
					continue;
				}

				if (!typeInfo.IsGenericType || 
					!typeInfo.BaseType.IsGenericType || 
					typeInfo.BaseType.GetGenericTypeDefinition() != typeof(VirtualFieldValueGenerator<,>))
				{
					continue;
				}

				var name = typeInfo.Name;
				var dashIndex = name.LastIndexOf('`');

				if (dashIndex != -1)
				{
					name = name.Substring(0, dashIndex);
				}

				var nameLC = name.ToLower();
				if (nameLC.EndsWith("valuegenerator"))
				{
					nameLC = nameLC.Substring(0, name.Length - 14);
					name = name.Substring(0, name.Length - 14);
				}

				// Got one. Add its type to the global include set.
				var valueGenerator = new ContentField(new VirtualInfo()
				{
					FieldName = name,
					IsList = false,
					ImplicitTypes = new List<Type>(), // Force these to be explicit.
					ValueGeneratorType = typeInfo.AsType()
				});

				_globalVirtualFields[nameLC] = valueGenerator;

			}

		}

		/// <summary>
		/// Creates a map for the given autoservice's instance type.
		/// Use aService.GetChangeField(..); rather than this directly.
		/// </summary>
		public ContentFields(AutoService service)
		{
			Service = service;
			InstanceType = service.InstanceType;
			BuildMap();
		}
		
		/// <summary>
		/// Creates a map for the given type.
		/// Use aService.GetChangeField(..); rather than this directly.
		/// </summary>
		public ContentFields(Type instanceType)
		{
			InstanceType = instanceType;
			BuildMap();
		}

		/// <summary>
		/// Gets a local virtual field of the given type, or null if it doesn't exist.
		/// </summary>
		/// <param name="ofType"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public ContentField GetVirtualField(Type ofType, string name)
		{
			if (_vList == null)
			{
				return null;
			}

			foreach (var virt in _vList)
			{
				if (virt.VirtualInfo.Type == ofType && virt.VirtualInfo.FieldName == name)
				{
					return virt;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the include set using the given str.
		/// </summary>
		/// <param name="includeString"></param>
		/// <returns></returns>
		public async ValueTask<IncludeSet> GetIncludeSet(string includeString)
		{
			if (string.IsNullOrEmpty(includeString))
			{
				return null;
			}

			var lowerIncludes = includeString.ToLower();

			if (!includeSets.TryGetValue(lowerIncludes, out IncludeSet result))
			{
				result = new IncludeSet(lowerIncludes, this);
				await result.Parse();
				includeSets[lowerIncludes] = result;
			}

			return result;
		}

		/// <summary>
		/// Raw field list.
		/// </summary>
		private List<ContentField> _list;
		
		/// <summary>
		/// Raw virtual field list.
		/// </summary>
		private List<ContentField> _vList;
		
		/// <summary>
		/// The underlying mapping.
		/// </summary>
		private Dictionary<string, ContentField> _nameMap;
		
		/// <summary>
		/// Meta field mapping.
		/// </summary>
		private Dictionary<string, ContentField> _metaMap;
		
		/// <summary>
		/// The underlying mapping.
		/// </summary>
		private Dictionary<string, ContentField> _vNameMap;

		/// <summary>
		/// Raw field list.
		/// </summary>
		public List<ContentField> List{
			get{
				return _list;
			}
		}

		/// <summary>
		/// Map of meta field name -> field.
		/// </summary>
		public Dictionary<string, ContentField> MetaFieldMap
		{
			get
			{
				return _metaMap;
			}
		}

		/// <summary>
		/// List of virtual fields.
		/// </summary>
		public List<ContentField> VirtualList
		{
			get
			{
				return _vList;
			}
		}

		/// <summary>
		/// Virtual field name mapped to entry on this type only, lowercase.
		/// </summary>
		public Dictionary<string, ContentField> LocalVirtualNameMap
		{
			get
			{
				return _vNameMap;
			}
		}

		/// <summary>
		/// Raw field map, lowercase.
		/// </summary>
		public Dictionary<string, ContentField> NameMap{
			get{
				return _nameMap;
			}
		}

		/// <summary>
		/// The db index list.
		/// </summary>
		public List<DatabaseIndexInfo> IndexList {
			get {
				return _indexSet;
			}
		}

		/// <summary>
		/// The name of the primary ListAs, if there is one.
		/// </summary>
		public string PrimaryMapName;
		
		/// <summary>
		/// The primary ListAs, if there is one.
		/// </summary>
		public ContentField PrimaryMap;

		private List<DatabaseIndexInfo> _indexSet;

		/// <summary>
		/// Attributes on the type itself.
		/// </summary>
		public List<System.Attribute> TypeAttributes;

		/// <summary>
		/// Gets the first attribute on the instanceType of the given type. Returns null if there isn't one.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetAttribute<T>() where T : class
		{
			if (TypeAttributes == null)
			{
				return null;
			}

			foreach (var attribs in TypeAttributes)
			{
				if (attribs is T)
				{
					return attribs as T;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the set of attributes on the type. Returns null if there isn't any.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public List<T> GetTypeAttributes<T>() where T : class
		{
			if (TypeAttributes == null)
			{
				return null;
			}

			var result = new List<T>();

			foreach (var attribs in TypeAttributes)
			{
				if (attribs is T)
				{
					result.Add(attribs as T);
				}
			}

			return result;
		}

		/// <summary>
		/// Gets the first match of any of the given field names. They must be lowercase. Null if none exist.
		/// </summary>
		/// <param name="fieldNames"></param>
		/// <returns></returns>
		public ContentField TryGetAnyOf(string[] fieldNames)
		{
			for (var i = 0; i < fieldNames.Length; i++)
			{
				if (_nameMap.TryGetValue(fieldNames[i], out ContentField result))
				{
					return result;
				}
			}

			return null;
		}

		private void BuildMap()
		{
			if (InstanceType == null)
			{
				// There is no type.
				return;
			}

			// Start collecting db indices:
			_indexSet = new List<DatabaseIndexInfo>();

			// Build attribute set:
			TypeAttributes = ContentField.BuildAttributes(InstanceType);

			// Add global listAs attribs, if there is any:
			var listAsSet = GetTypeAttributes<ListAsAttribute>();

			// Get the implicit types, if there are any:
			var implicitSet = GetTypeAttributes<ImplicitForAttribute>();

			List<ContentField> listAsFields = null;

			if (listAsSet != null)
			{
				ListAsAttribute primary = null;

				foreach (var listAs in listAsSet)
				{
					List<Type> implicitTypes = null;

					foreach(var implicitAttrib in implicitSet)
					{
						if (implicitAttrib.ListAsName == listAs.FieldName)
						{
							if (implicitTypes == null)
							{
								implicitTypes = new List<Type>();
							}

							implicitTypes.Add(implicitAttrib.Type);
						}
					}

					if (listAs.Explicit && implicitTypes == null)
					{
						implicitTypes = new List<Type>();
					}

					var listAsField = new ContentField(new VirtualInfo()
					{
						FieldName = listAs.FieldName,
						Type = InstanceType,
						ImplicitTypes = implicitTypes,
						IsList = true,
						IdSourceField = "Id"
					});

					if (listAs.IsPrimary)
					{
						if (primary != null)
						{
							throw new Exception(
								"Multiple primary ListAs attributes specified on type '" + InstanceType.Name + 
								"'. If a type has more than one ListAs, only one can be the primary one. " + 
								primary.FieldName + " and " + listAs.FieldName + " are currently both set to primary. Use IsPrimary=false on one of them."
							);
						}

						primary = listAs;
						PrimaryMapName = listAs.FieldName;
						PrimaryMap = listAsField;
					}

					GlobalVirtualFields[listAs.FieldName.ToLower()] = listAsField;

					if (listAsFields == null)
					{
						listAsFields = new List<ContentField>();
					}

					listAsFields.Add(listAsField);
				}
			}



			// Public fields:
			var fields = InstanceType.GetFields();

			_nameMap = new Dictionary<string, ContentField>();
			_metaMap = new Dictionary<string, ContentField>();
			_list = new List<ContentField>();
			_vNameMap = new Dictionary<string, ContentField>();
			_vList = new List<ContentField>();

			for (var i=0;i<fields.Length;i++){
				var field = fields[i];
				var cf = new ContentField(field);
				_list.Add(cf);
				cf.Id = _list.Count;
				_nameMap[field.Name.ToLower()] = cf;
				
				// Get field attributes:
				var attribs = cf.Attributes;

				foreach (var attrib in attribs)
				{
					if (attrib is DatabaseIndexAttribute attribute)
					{
						// Add db index:
						var dbi = new DatabaseIndexInfo(attribute, new ContentField[] { cf });
						dbi.Id = _indexSet.Count;
						cf.AddIndex(dbi);
						_indexSet.Add(dbi);
					}

					if (attrib is LocalizedAttribute)
					{
						cf.Localised = true;
					}

					if (attrib is MetaAttribute)
					{
						_metaMap[(attrib as MetaAttribute).FieldName.ToLower()] = cf;
					}
				}
			}

			// Do we have a title and description meta field?
			// If not, we'll attempt to invent them based on some common names.
			if (!_metaMap.ContainsKey("title"))
			{
				var titleField = TryGetAnyOf(CommonTitleNames);
				if (titleField != null)
				{
					_metaMap["title"] = titleField;
				}
			}

			if (!_metaMap.ContainsKey("description"))
			{
				var descriptionField = TryGetAnyOf(CommonDescriptionNames);
				if (descriptionField != null)
				{
					_metaMap["description"] = descriptionField;
				}
			}

			// Collect any databaseIndex attributes on the type itself:
			var attributes = TypeAttributes;

			foreach (var attrib in attributes)
			{
				if (attrib is DatabaseIndexAttribute attribute)
				{
					var indexFields = attribute.Fields;

					if (indexFields == null || indexFields.Length == 0)
					{
						throw new ArgumentException("You've got a [DatabaseIndex] on " + InstanceType.Name + " which requires fields but has none.");
					}

					var columnFields = new ContentField[indexFields.Length];

					for (var i = 0; i < indexFields.Length; i++)
					{
						if (!_nameMap.TryGetValue(indexFields[i].ToLower(), out ContentField reffedIndexField))
						{
							// All the reffed fields must exist.
							throw new ArgumentException(
								"A [DatabaseIndex] on '" + InstanceType.Name + "' tries to use a field called '" + indexFields[i] + "' which doesn't exist. " +
								"Note that properties can't be used in an index."
							);
						}

						columnFields[i] = reffedIndexField;
					}

					var dbi = new DatabaseIndexInfo(attribute, columnFields);
					dbi.Id = _indexSet.Count;

					for (var i = 0; i < columnFields.Length; i++)
					{
						columnFields[i].AddIndex(dbi);
					}

					_indexSet.Add(dbi);
				}
			}

			var properties = InstanceType.GetProperties();
			
			// Public properties (can't be localised):
			for(var i=0;i<properties.Length;i++){
				var property = properties[i];
				var cf = new ContentField(property);
				_list.Add(cf);
				cf.Id = _list.Count;
				_nameMap[property.Name.ToLower()] = cf;
			}

			// Get all the virtuals:
			var virtualFields = GetTypeAttributes<HasVirtualFieldAttribute>();

			foreach (var fieldMeta in virtualFields)
			{
				var virtualFieldType = fieldMeta.Type;
				string virtualFieldTypeName = null;
				AutoService knownService = null;

				if (virtualFieldType == null)
				{
					if (string.IsNullOrEmpty(fieldMeta.TypeSourceField))
					{
						continue;
					}

					// Is it a field on the type?
					if (_nameMap.TryGetValue(fieldMeta.TypeSourceField.ToLower(), out ContentField targetField))
					{
						// This field must be a string (holding type names) or an int (contentTypeId).
						if (targetField.FieldType == typeof(string) || targetField.FieldType == typeof(int))
						{
							// So far so good!

							// The ID field MUST be a ulong.
							ContentField idSource;
							if (!string.IsNullOrEmpty(fieldMeta.IdSourceField) && _nameMap.TryGetValue(fieldMeta.IdSourceField.ToLower(), out idSource))
							{

								if (idSource.FieldType == typeof(ulong))
								{
									// OK - all checks passed.
									// We actually have a field which holds either a contentTypeId or content type names.
								}

							}
							
						}
					}

					// Attempt to get the type by name instead:
					var relatedService = Services.Get(fieldMeta.TypeSourceField + "Service");

					if (relatedService != null)
					{
						virtualFieldType = relatedService.InstanceType;
						knownService = relatedService;
					}
					else
					{
						// Lazy load it later. The service might be a dynamic one which simply hasn't loaded yet.
						virtualFieldTypeName = fieldMeta.TypeSourceField;
					}
				}

				var vInfo = new VirtualInfo()
				{
					FieldName = fieldMeta.FieldName,
					Type = virtualFieldType,
					TypeName = virtualFieldTypeName,
					IdSourceField = fieldMeta.IdSourceField,
					IsList = fieldMeta.List,
					Service = knownService
				};

				if (virtualFieldType != null && vInfo.IsList)
				{
					// Establish the meta title field name straight away.
					vInfo.SetupMetaTitle();
				}

				var cf = new ContentField(vInfo);

				// Resolve ID sources:
				if (!string.IsNullOrEmpty(vInfo.IdSourceField))
				{
					if (!_nameMap.TryGetValue(vInfo.IdSourceField.ToLower(), out vInfo.IdSource))
					{
						throw new PublicException("A field called '" + vInfo.IdSourceField + "' doesn't exist as requested by virtual field '" + vInfo.FieldName + "' on type " + InstanceType.Name, "vfield_require_doesnt_exist");
					}

					if (vInfo.IdSource != null && vInfo.IdSource.UsedByVirtual == null)
					{
						vInfo.IdSource.UsedByVirtual = cf;
					}
				}

				_vList.Add(cf);
				cf.Id = _vList.Count;
				_vNameMap[vInfo.FieldName.ToLower()] = cf;
			}

			if (listAsFields != null)
			{
				// Mark its meta title field.
				if (_metaMap.TryGetValue("title", out ContentField cf))
				{
					for (var i = 0; i < listAsFields.Count; i++)
					{
						listAsFields[i].VirtualInfo.MetaTitleField = cf;
					}
				}
			}

		}

		/// <summary>
		/// Attempts to get the named field.
		/// </summary>
		public bool TryGetValue(string fieldName, out ContentField field){
			return _nameMap.TryGetValue(fieldName, out field);
		}
		
	}
	
	/// <summary>
	/// A field or property on a Content type.
	/// </summary>
	public partial class ContentField
	{
		/// <summary>
		/// The depth of a virtual field. "A.B" has a depth of 1, "A" a depth of 0 and "A.B.C" a depth of 2.
		/// </summary>
		public int VirtualDepth;

		/// <summary>
		/// The first virtual field that this field is used by (applies to local mappings only).
		/// </summary>
		public ContentField UsedByVirtual;

		/// <summary>
		/// True if this field is [Localised]
		/// </summary>
		public bool Localised;

		/// <summary>
		/// This fields ID. It also directly represents the change flag.
		/// </summary>
		public int Id{
			get{
				return _id;
			}
			set{
				_id = value;
			}
		}

		/// <summary>
		/// The type an ID collector uses. This is generated.
		/// </summary>
		public Type IDCollectorType
		{
			get {
				return _idCollectorType;
			}
		}
		/// <summary>
		/// Converts a set of attribute data from the given type (including any it inherits) into an attribute list.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static List<Attribute> BuildAttributes(Type type)
		{
			var result = new List<Attribute>();
			BuildAttributes(type.CustomAttributes, result);

			var baseType = type.BaseType;

			while (baseType != null && baseType != typeof(object))
			{
				BuildAttributes(baseType.CustomAttributes, result);
				baseType = baseType.BaseType;
			}

			return result;
		}

		/// <summary>
		/// Converts a set of attribute data into an attribute list.
		/// </summary>
		/// <param name="customAttributes"></param>
		/// <param name="intoList"></param>
		/// <returns></returns>
		public static List<Attribute> BuildAttributes(IEnumerable<CustomAttributeData> customAttributes, List<Attribute> intoList = null)
		{
			// Can't use field.GetCustomAttributes because it doesn't work for dynamically generated types.
			// field.CustomAttributes works either way though, we just have to construct the attribute for ourselves.

			List<Attribute> attribs = intoList == null ? new List<Attribute>() : intoList;

			if (customAttributes != null)
			{
				foreach (var ca in customAttributes)
				{
					var paramCount = ca.ConstructorArguments.Count;
					var ctorParSet = new object[paramCount];

					for (var i = 0; i < paramCount; i++)
					{
						var argInfo = ca.ConstructorArguments[i];
						var value = argInfo.Value;

						if(value is ICollection<CustomAttributeTypedArgument> col)
						{
							// Convert to a string[].
							var strSet = new string[col.Count];
							var index = 0;

							foreach (var entry in col)
							{
								strSet[index++] = (string)(entry.Value);
							}

							value = strSet;
						}

						ctorParSet[i] = value;
					}

					var ctor = ca.Constructor;

					var newAttrib = ctor.Invoke(ctorParSet) as Attribute;

					// Need to set each named arg too.
					paramCount = ca.NamedArguments.Count;

					for (var i = 0; i < paramCount; i++)
					{
						var argInfo = ca.NamedArguments[i];

						// Get the field or property:
						var member = argInfo.MemberInfo;

						// Set it on the attrib:
						var fldInfo = member as FieldInfo;

						if (fldInfo != null)
						{
							fldInfo.SetValue(newAttrib, argInfo.TypedValue.Value);
						}
						else
						{
							var prop = member as PropertyInfo;

							if (prop != null)
							{
								prop.GetSetMethod().Invoke(newAttrib, new object[] { argInfo.TypedValue.Value });
							}
						}

					}

					attribs.Add(newAttrib);
				}
			}

			return attribs;
		}

		/// <summary>
		/// IDCollector concrete type for this field, if it represents some kind of ID. 
		/// This collector type is generated and reads the value of this field from a given object.
		/// </summary>
		private Type _idCollectorType;

		/// <summary>
		/// First ID collector in the pool for this field.
		/// </summary>
		private IDCollector FirstInPool;

		/// <summary>
		/// ID collector pool lock.
		/// </summary>
		private object IDCollectorPoolLock = new object();

		/// <summary>
		/// Gets an ID collector from a pool.
		/// </summary>
		/// <returns></returns>
		public IDCollector RentCollector()
		{
			IDCollector instance = null;

			lock (IDCollectorPoolLock)
			{
				if (FirstInPool != null)
				{
					// Pop from the pool:
					instance = FirstInPool;
					FirstInPool = instance.NextCollector;
				}
			}

			if (instance == null)
			{
				// Instance one:
				instance = Activator.CreateInstance(IDCollectorType) as IDCollector;
				instance.Pool = this;
			}

			instance.NextCollector = null;
			return instance;
		}

		/// <summary>
		/// Returns the given collector to the pool. This also internally releases the collector's buffers.
		/// </summary>
		/// <param name="collector"></param>
		public void AddToPool(IDCollector collector)
		{
			// Re-add to this pool:
			lock (IDCollectorPoolLock)
			{
				collector.NextCollector = FirstInPool;
				FirstInPool = collector;
			}
		}

		/// <summary>
		/// Sets the ID collector type.
		/// </summary>
		/// <param name="type"></param>
		public void SetIDCollectorType(Type type)
		{
			_idCollectorType = type;
		}

		/// <summary>
		/// True if this is a virtual field.
		/// </summary>
		public bool IsVirtual
		{
			get {
				return VirtualInfo != null;
			}
		}

		/// <summary>
		/// This fields ID. It also directly represents the change flag.
		/// </summary>
		private int _id;
		
		/// <summary>
		/// Underlying field (can be null if it's a property).
		/// </summary>
		public FieldInfo FieldInfo;
		
		/// <summary>
		/// Underlying propertyInfo (can be null if it's a field).
		/// </summary>
		public PropertyInfo PropertyInfo;

		/// <summary>
		/// Virtual field information.
		/// </summary>
		public VirtualInfo VirtualInfo;

		/// <summary>
		/// Set if this field is used by any database indices. Only available on fields, not properties.
		/// </summary>
		public List<DatabaseIndexInfo> UsedByIndices;

		/// <summary>
		/// Attributes on the field/ property (if any). Can be null.
		/// </summary>
		public List<Attribute> Attributes;

		/// <summary>
		/// Adds an index to the usedByIndices set. Does not check if it was already in there.
		/// </summary>
		/// <param name="index"></param>
		public void AddIndex(DatabaseIndexInfo index)
		{
			if (UsedByIndices == null)
			{
				UsedByIndices = new List<DatabaseIndexInfo>();
			}

			UsedByIndices.Add(index);
		}

		/// <summary>
		/// Gets the "local"
		/// </summary>
		/// <param name="relativeTo"></param>
		/// <returns></returns>
		public ContentField GetIdFieldIfMappingNotRequired(ContentFields relativeTo)
		{

			// Do we need to map? Often yes, but occasionally not necessary.
			// We don't if the target type has a virtual field of the source type, where the virtual field name is simply the same as the instance type
			return VirtualInfo.Service.GetContentFields().GetVirtualField(relativeTo.InstanceType, relativeTo.InstanceType.Name);

		}

		/// <summary>
		/// Gets the mapping service for a virtual list field. Can be null if one isn't actually necessary.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<MappingInfo> GetOptionalMappingService(ContentFields relativeTo)
		{
			var fieldOfType = GetIdFieldIfMappingNotRequired(relativeTo);

			if (fieldOfType != null)
			{
				// No mapping needed - the mapping is instead to use this virtual field.
				return new MappingInfo {
					Service = null,
					TargetField = fieldOfType,
					TargetFieldName = fieldOfType.VirtualInfo.IdSource.Name
				};
			}

			var mappingService = await GetMappingService(relativeTo);

			// We need to know what the target field is as we'll need a collector on it.
			var mappingContentFields = mappingService.GetContentFields();

			// Try to get target field (e.g. TagId):
			if (!mappingContentFields.TryGetValue("targetid", out ContentField targetField))
			{
				throw new Exception("Couldn't find target field on a mapping type. This indicates an issue with the mapping engine rather than your usage.");
			}

			return new MappingInfo
			{
				Service = mappingService,
				TargetField = targetField,
				TargetFieldName = targetField.Name
			};
		}

		/// <summary>
		/// Gets a mapping service but doesn't consider if it is optional.
		/// It would be optional if the mapped from type has an ID field that relates to the mapped to type.
		/// </summary>
		/// <param name="relativeTo"></param>
		/// <returns></returns>
		public async ValueTask<AutoService> GetMappingService(ContentFields relativeTo)
		{
			var svc = VirtualInfo.Service;
			return await MappingTypeEngine.GetOrGenerate(relativeTo.Service, svc, VirtualInfo.FieldName);
		}

		/// <summary>
		/// </summary>
		public ContentField(FieldInfo info){
			FieldInfo = info;
			Attributes = BuildAttributes(info.CustomAttributes);
		}
		
		/// <summary>
		/// </summary>
		public ContentField(PropertyInfo info){
			PropertyInfo = info;
			Attributes = BuildAttributes(info.CustomAttributes);
		}

		/// <summary>
		/// Virtual field.
		/// </summary>
		public ContentField(VirtualInfo info)
		{
			VirtualInfo = info;
		}

		/// <summary>
		/// Gets the first attribute on this field of the given type. Returns null if there isn't one.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetAttribute<T>() where T:class
		{
			if (Attributes == null)
			{
				return null;
			}

			foreach (var attribs in Attributes)
			{
				if (attribs is T)
				{
					return attribs as T;
				}
			}

			return null;
		}
		
		/// <summary>
		/// Gets the set of attributes on this field of the given type. Returns null if there isn't one.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public List<T> GetAttributes<T>() where T:class
		{
			if (Attributes == null)
			{
				return null;
			}

			var result = new List<T>();

			foreach (var attribs in Attributes)
			{
				if (attribs is T)
				{
					result.Add(attribs as T);
				}
			}

			return result;
		}

		/// <summary>
		/// True if this field is highspeed indexable - either it's used by a virtual field 
		/// (the index is the map for that vfield), or it's e.g. the Id field.
		/// </summary>
		public bool IsIndexable
		{
			get
			{
				return UsedByIndices != null || UsedByVirtual != null;
			}
		}

		/// <summary>
		/// Field name
		/// </summary>
		public string Name
		{
			get {
				if (FieldInfo != null)
				{
					return FieldInfo.Name;
				}

				if (PropertyInfo != null)
				{
					return PropertyInfo.Name;
				}

				return VirtualInfo.FieldName;
			}
		}

		/// <summary>
		/// Field value type
		/// </summary>
		public Type FieldType
		{
			get
			{
				if (FieldInfo != null)
				{
					return FieldInfo.FieldType;
				}

				if (PropertyInfo != null)
				{
					return PropertyInfo.PropertyType;
				}

				// Unavailable on virt fields.
				return null;
			}
		}
	}


	/// <summary>
	/// Meta about a particular map to use (in reverse, target->source)
	/// </summary>
	public struct ReverseMappingInfo
	{
		/// <summary>
		/// The service. Will always exist.
		/// </summary>
		public AutoService Service;
		/// <summary>
		/// The source ID field.
		/// </summary>
		public ContentField SourceField;
	}
	
	/// <summary>
	/// Meta about a particular map to use.
	/// </summary>
	public struct MappingInfo
	{
		/// <summary>
		/// The service, if there is one. This is a MappingService.
		/// </summary>
		public AutoService Service;
		/// <summary>
		/// The target ID field.
		/// </summary>
		public ContentField TargetField;
		/// <summary>
		/// The name of the target field.
		/// </summary>
		public string TargetFieldName;
	}

	/// <summary>
	/// 
	/// </summary>
	public class VirtualInfo
	{
		/// <summary>
		/// The effective name of the field.
		/// </summary>
		public string FieldName;
		
		/// <summary>
		/// Sometimes Type is not available until later. This is the name of the type to load on demand.
		/// </summary>
		public string TypeName;

		/// <summary>
		/// If this virtual field is a value generator, this is the generic class which will be instanced.
		/// </summary>
		public Type ValueGeneratorType;

		/// <summary>
		/// Exists if this is an explicit ListAs field. This is the set of source types for which the ListAs * is implicit.
		/// </summary>
		public List<Type> ImplicitTypes;

		/// <summary>
		/// Don't use this ListAs field in an * if it is explicit, and the source type is not in the ImplicitTypes set.
		/// </summary>
		public bool IsExplicit
		{
			get {
				return ImplicitTypes != null;
			}
		}

		/// <summary>
		/// Usually a one-off to establish if this ListAs field is implicit for the given source type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public bool IsImplicitFor(Type type)
		{
			return ImplicitTypes != null && ImplicitTypes.Contains(type);
		}

		/// <summary>
		/// The meta="title" field for this listAs field. Used for searching through them.
		/// </summary>
		public ContentField MetaTitleField;

		/// <summary>
		/// True if list
		/// </summary>
		public bool IsList;

		/// <summary>
		/// The type of the content in this field. T[] indicates an array.
		/// </summary>
		public Type FieldType;

		/// <summary>
		/// Set only if Type is not null.
		/// </summary>
		public AutoService _service;

		private Type _type;

		/// <summary>
		/// Sets up the MetaTitleField on list fields.
		/// </summary>
		public void SetupMetaTitle()
		{
			if (!IsList)
			{
				return;
			}

			var svc = Service;

			if (svc != null)
			{
				var contentFields = svc.GetContentFields();

				if (contentFields.MetaFieldMap.TryGetValue("title", out ContentField cf))
				{
					MetaTitleField = cf;
				}
			}
		}

		/// <summary>
		/// The type that the ID is for. Must be provided.
		/// </summary>
		public Type Type {
			get {

				if (_type == null)
				{
					if (TypeName == null)
					{
						throw new Exception("Type name required if lazy loading the type of a virtual field (" + FieldName + ")");
					}

					if (_service == null)
					{
						_service = Services.Get(TypeName + "Service");
					}

					if (_service == null)
					{
						// Type does not exist when it is absolutely necessary.
						throw new Exception(
							"A virtual field (" + FieldName + ") referring to another type (" + TypeName + 
							") was not able to resolve because it either does not exist or has not loaded yet."
						);
					}

					_type = _service.InstanceType;

					// Setup meta title if one is necessary:
					SetupMetaTitle();
				}

				return _type;
			}
			set {
				_type = value;
			}
		}

		/// <summary>
		/// The field on the class that the ID of the optional object comes from.
		/// </summary>
		public string IdSourceField;

		/// <summary>
		/// Resolved ID source field.
		/// </summary>
		public ContentField IdSource;

		/// <summary>
		/// The service for the type (if there is a type).
		/// </summary>
		public AutoService Service {
			get {
				if (_service != null)
				{
					return _service;
				}

				_service = Services.GetByContentType(Type);
				return _service;
			}
			set
			{
				if (value != null)
				{
					_service = value;
				}
			}
		}

	}

}