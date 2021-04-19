
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Api.Startup {
	
	/// <summary>
	/// Tracks fields which have changed, without allocating anything. 
	/// Note that this is field specific (because only fields are persistent). Properties do not work.
	/// Limited to 64 fields on an object.
	/// </summary>
	public struct ChangedFields {
		
		/// <summary>
		/// Represents all fields will be updated.
		/// </summary>
		public static readonly ChangedFields All = new ChangedFields(ulong.MaxValue);
		
		/// <summary>
		/// Internal bitmap of the fields.
		/// </summary>
		private ulong FieldMap1;
		
		
		/// <summary>
		/// Specific map of fields.
		/// </summary>
		public ChangedFields(ulong map){
			FieldMap1 = map;
		}

		/// <summary>
		/// True if this set contains the given one.
		/// </summary>
		/// <param name="fields"></param>
		/// <returns></returns>
		public bool Contains(ChangedFields fields)
		{
			return (FieldMap1 & fields.FieldMap1) == fields.FieldMap1;
		}

		/// <summary>
		/// Adds a field to the change set.
		/// </summary>
		public static ChangedFields operator + (ChangedFields a, ChangedFields b) {
			
			// Simply bitwise compose the two ulongs together:
			return new ChangedFields(a.FieldMap1 | b.FieldMap1);
			
		}
		
	}
	
	/// <summary>
	/// Composable change fields. These do allocate so are best used once for maximum throughput.
	/// </summary>
	public class ComposableChangeField
	{
		/// <summary>
		/// The raw map of fields from the source type.
		/// </summary>
		public ContentFields Map;
		
		/// <summary>
		/// The underlying non-allocated fields.
		/// </summary>
		public ChangedFields ChangeFields = new ChangedFields(0);
		
		/// <summary>
		/// Can treat a ComposableChangeField as if it were a ChangedFields.
		/// </summary>
		public static implicit operator ChangedFields(ComposableChangeField ccf) => ccf.ChangeFields;
		
		/// <summary>
		/// Adds the given named field into this change field set.
		/// </summary>
		public ComposableChangeField And(string name)
		{
			if(!Map.TryGetValue(name.ToLower(),out ContentField field))
			{
				throw new Exception("A field called '" + name + "' doesn't exist on the type " + Map.InstanceType.Name);
			}
			
			// Add it:
			ChangeFields += field.ChangeFlag;
			
			return this;
		}
	}
	
	/// <summary>
	/// A map of field name -> field on a Content type. 
	/// This level does not consider e.g. role accessibility - it is just a raw, complete set of fields and properties, including virtual ones.
	/// </summary>
	public class ContentFields
	{
		/// <summary>
		/// Global virtual fields. ListAs appears in here.
		/// </summary>
		public static Dictionary<string, ContentField> _globalVirtualFields = new Dictionary<string, ContentField>();

		/// <summary>
		/// Inclusion sets that have been pre-generated, rooted from this set.
		/// </summary>
		public Dictionary<string, IncludeSet> includeSets = new Dictionary<string, IncludeSet>();

		/// <summary>
		/// The AutoService that this map is for.
		/// </summary>
		public AutoService Service;

		/// <summary>
		/// The type that this map is for.
		/// </summary>
		public Type InstanceType;
		
		/// <summary>
		/// Creates a map for the given type.
		/// Use aService.GetChangeField(..); rather than this directly.
		/// </summary>
		public ContentFields(AutoService service)
		{
			Service = service;
			InstanceType = service.InstanceType;
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

		private void BuildMap()
		{
			if (InstanceType == null)
			{
				// There is no type.
				return;
			}

			// Add global listAs, if there is one:
			var listAs = InstanceType.GetCustomAttribute<ListAsAttribute>();

			if (listAs != null)
			{
				_globalVirtualFields[listAs.FieldName.ToLower()] = new ContentField(new VirtualInfo() {
					FieldName = listAs.FieldName,
					Type = InstanceType,
					IsList = true,
					IdSourceField = "Id"
				});
			}

			// Public fields:
			var fields = InstanceType.GetFields();

			_nameMap = new Dictionary<string, ContentField>();
			_list = new List<ContentField>();
			_vNameMap = new Dictionary<string, ContentField>();
			_vList = new List<ContentField>();

			for (var i=0;i<fields.Length;i++){
				var field = fields[i];
				var cf = new ContentField(field);
				_list.Add(cf);
				cf.Id = _list.Count;
				_nameMap[field.Name.ToLower()] = cf;
			}
			
			var properties = InstanceType.GetProperties();
			
			// Public properties:
			for(var i=0;i<properties.Length;i++){
				var property = properties[i];
				var cf = new ContentField(property);
				_list.Add(cf);
				cf.Id = _list.Count;
				_nameMap[property.Name.ToLower()] = cf;
			}

			// Get all the virtuals:
			var virtualFields = InstanceType.GetCustomAttributes<HasVirtualFieldAttribute>();

			foreach (var fieldMeta in virtualFields)
			{
				var vInfo = new VirtualInfo()
				{
					FieldName = fieldMeta.FieldName,
					Type = fieldMeta.Type,
					IdSourceField = fieldMeta.IdSourceField
				};

				if (vInfo.Type == null)
				{
					throw new PublicException("Virtual fields require a type. '" + vInfo.FieldName + "' on '" + InstanceType.Name + "' does not have one.", "field_type_required");
				}

				// Resolve ID sources:
				if (!string.IsNullOrEmpty(vInfo.IdSourceField))
				{
					if (!_nameMap.TryGetValue(vInfo.IdSourceField.ToLower(), out vInfo.IdSource))
					{
						throw new PublicException("A field called '" + vInfo.IdSourceField + "' doesn't exist as requested by virtual field '" + vInfo.FieldName + "' on type " + InstanceType.Name, "vfield_require_doesnt_exist");
					}
				}

				var cf = new ContentField(vInfo);
				_vList.Add(cf);
				cf.Id = _vList.Count;
				_vNameMap[vInfo.FieldName.ToLower()] = cf;
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
		/// This fields ID. It also directly represents the change flag.
		/// </summary>
		public int Id{
			get{
				return _id;
			}
			set{
				_id = value;
				_changeFlag = new ChangedFields((ulong)1<<(_id-1));
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
		/// Shallow clones this field. 
		/// </summary>
		/// <returns></returns>
		public ContentField Clone() {
			var cf = new ContentField(FieldInfo);
			cf.PropertyInfo = PropertyInfo;
			cf.VirtualInfo = VirtualInfo;
			cf._id = _id;
			cf._changeFlag = _changeFlag;
			return cf;
		}

		/// <summary>
		/// The flag used to indicate this field has changed.
		/// </summary>
		public ChangedFields ChangeFlag{
			get{
				return _changeFlag;
			}
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
		/// The flag used to indicate this field has changed.
		/// </summary>
		private ChangedFields _changeFlag;
		
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
		/// </summary>
		public ContentField(FieldInfo info){
			FieldInfo = info;
		}
		
		/// <summary>
		/// </summary>
		public ContentField(PropertyInfo info){
			PropertyInfo = info;
		}

		/// <summary>
		/// Virtual field.
		/// </summary>
		public ContentField(VirtualInfo info)
		{
			VirtualInfo = info;
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
	/// 
	/// </summary>
	public class VirtualInfo
	{
		/// <summary>
		/// The effective name of the field.
		/// </summary>
		public string FieldName;

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

		/// <summary>
		/// The type that the ID is for. Must be provided.
		/// </summary>
		public Type Type;

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
		}
	}

}