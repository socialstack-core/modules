
using Api.Contexts;
using Api.Database;
using Api.SocketServerLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Startup
{
	
	/// <summary>
	/// Represents a set of inclusions. Assumes that most include strings will very rarely vary.
	/// </summary>
	public class IncludeSet
	{
		/// <summary>
		/// The raw include string (lowercase).
		/// </summary>
		public string IncludeString;

		/// <summary>
		/// Content field set that this include set is relative to.
		/// </summary>
		public ContentFields RelativeTo;

		/// <summary>
		/// A set of inclusions for the given parent type and include string.
		/// </summary>
		public IncludeSet(string includeString, ContentFields relativeTo)
		{
			IncludeString = includeString;
			RelativeTo = relativeTo;
		}

		/// <summary>
		/// An allocating call which builds the tree of includes.
		/// </summary>
		public async ValueTask Parse()
		{
			RootInclude = new InclusionNode(RelativeTo, null);
			RootInclude.Service = RelativeTo.Service;

			int start = 0;
			var comma = IncludeString.IndexOf(',');
			
			while (comma != -1)
			{
				var fieldName = IncludeString.Substring(start, comma - start);

				// Add the given field:
				await Add(fieldName);

				// Next comma:
				start = comma + 1;
				comma = IncludeString.IndexOf(',', start);
			}

			await Add(start == 0 ? IncludeString : IncludeString.Substring(start));

			// Bake the root:
			int outputIndex = -1;
			RootInclude.Bake(ref outputIndex);
		}

		/// <summary>
		/// Root inclusion.
		/// </summary>
		public InclusionNode RootInclude;

		/// <summary>
		/// Adds a field to the set. It can contain a * (or be just "*"), but not after a mixed content type field.
		/// </summary>
		/// <param name="rootRelativeFieldName"></param>
		private async ValueTask Add(string rootRelativeFieldName)
		{
			var pieces = rootRelativeFieldName.Split('.');

			InclusionNode current = RootInclude;

			for (var i = 0; i < pieces.Length; i++)
			{
				var currentName = pieces[i];

				if (current == null)
				{
					// primaryUrl.thing (not possible).
					throw new PublicException("Nested includes are not available on functional includes.", "functional_include_nested");
				}

				if (currentName == "*")
				{
					// Every field on the current relative to. This can't go on a mixed content type, and it can only ever be the last piece.
					if (i != pieces.Length - 1)
					{
						throw new PublicException("Wildcard inclusions (*) can only be the last field in an inclusion string. 'Tags.*.Thing' is invalid, for example.", "wildcard_include_last");
					}

					// Add _every_ virtual field in RelativeTo.
					// (Including globals, relative to it).
					foreach (var field in current.RelativeTo.VirtualList)
					{
						await current.Add(field, rootRelativeFieldName);
					}

					// Add every ListAs field except for the explicit ones where this is not an implicit type.
					foreach (var kvp in ContentFields.GlobalVirtualFields)
					{
						var virtInfo = kvp.Value.VirtualInfo;

						if (virtInfo != null && virtInfo.IsExplicit)
						{
							// Check if this is one of the implicit types.
							if (!virtInfo.IsImplicitFor(current.RelativeTo.InstanceType))
							{
								// Skip!
								continue;
							}
						}

						await current.Add(kvp.Value, rootRelativeFieldName);
					}

					// Can't continue (at least, not in this version) because 'current' is now a set of nodes.
					break;
				}

				// Is it a global field?
				if (ContentFields.GlobalVirtualFields.TryGetValue(currentName, out ContentField globalField))
				{
					// Yes! it's a global - these ones are available on all types. They're usually lists, like tags or categories.
					// Service and type are required for these.
					current = await current.Add(globalField, rootRelativeFieldName);
					continue;
				}

				// It must be a local field, otherwise it doesn't exist:
				if (!current.RelativeTo.LocalVirtualNameMap.TryGetValue(currentName, out ContentField localField))
				{
					throw new PublicException("Your request tried to use '" + currentName + "' in include '" + rootRelativeFieldName +  "' but it doesn't exist", "include_no_exist");
				}

				// Add local field:
				current = await current.Add(localField, rootRelativeFieldName);
			}

		}
		
	}

	/// <summary>
	/// A particular inclusion node in the tree of nodes.
	/// </summary>
	public class InclusionNode
	{
		/// <summary>
		/// The field that sources data for this inclusion node.
		/// </summary>
		public ContentField HostField;

		/// <summary>
		/// The service to use to resolve the actual value of this node.
		/// </summary>
		public AutoService Service;

		/// <summary>
		/// Included as.
		/// </summary>
		public string IncludeName;
		
		/// <summary>
		/// Set of unique children, by lowercase field name.
		/// </summary>
		public Dictionary<string, InclusionNode> UniqueChildNodes = new Dictionary<string, InclusionNode>();

		/// <summary>
		/// Set of unique functional children, by lowercase field name.
		/// </summary>
		private Dictionary<string, FunctionalInclusionNode> UniqueFunctionalIncludes = new Dictionary<string, FunctionalInclusionNode>();

		/// <summary>
		/// Created during the Bake() call.
		/// </summary>
		public InclusionNode[] ChildNodes;

		/// <summary>
		/// The set of functional includes for this node, if there are any (can be null, but won't be an empty array).
		/// </summary>
		public FunctionalInclusionNode[] FunctionalIncludes;

		/// <summary>
		/// ID collector to use.
		/// </summary>
		public int CollectorIndex = -1;

		/// <summary>
		/// Relative set
		/// </summary>
		public ContentFields RelativeTo;

		/// <summary>
		/// Id fields to create collectors for whilst this include node is being executed.
		/// </summary>
		public ContentField[] IdFields;

		/// <summary>
		/// The include header for this inclusion node.
		/// </summary>
		private byte[] _includeHeader;

		/// <summary>
		/// The inclusion header. Ends with a map for ListAs. {"name":"Thing.Tags","fieldName":"tags","on":0,"map":[
		/// </summary>
		public byte[] IncludeHeader {
			get
			{
				return _includeHeader;
			}
		}

		/// <summary>
		/// Parent node.
		/// </summary>
		public InclusionNode Parent;

		/// <summary>
		/// The index of this inclusion in the output inclusion array.
		/// </summary>
		public int InclusionOutputIndex = -1;

		/// <summary>
		/// E.g. TagId - the field in a mapping row that represents the target object. This must be collected as well.
		/// </summary>
		public ContentField MappingTargetField;

		/// <summary>
		/// Mapping target field name
		/// </summary>
		public string MappingTargetFieldName;

		/// <summary>
		/// The mapping service for this list node.
		/// </summary>
		public AutoService MappingService;

		/// <summary>
		/// Create a new node
		/// </summary>
		/// <param name="relativeTo"></param>
		/// <param name="parent"></param>
		public InclusionNode(ContentFields relativeTo, InclusionNode parent)
		{
			RelativeTo = relativeTo;
			Parent = parent;
		}

		/// <summary>
		/// Sets this include node as a ListAs with the given header info.
		/// </summary>
		/// <param name="includedAs">The raw text in the include string that this include node came from.</param>
		/// <param name="listAs"></param>
		public void SetHeader(string includedAs, string listAs)
		{
			if (string.IsNullOrEmpty(listAs))
			{
				throw new Exception("Can't ListAs() a blank field name. It's required.");
			}

			// lc first:
			var fieldName = char.ToLower(listAs[0]) + listAs.Substring(1);

			var header = "{\"name\":\"" + includedAs + "\",\"field\":\"" + fieldName + "\"";

			if (Parent.InclusionOutputIndex != -1)
			{
				header += ",\"on\":" + Parent.InclusionOutputIndex;
			}

			if (MappingService == null)
			{
				var mapField = char.ToLower(MappingTargetFieldName[0]) + MappingTargetFieldName.Substring(1);
				header += ",\"map\":\"" + mapField + "\",\"values\":[";
			}
			else
			{
				header += ",\"map\":[";
			}

			_includeHeader = System.Text.Encoding.UTF8.GetBytes(header);
		}

		/// <summary>
		/// Sets this include node as a virtual field with the given header info.
		/// </summary>
		/// <param name="includedAs"></param>
		/// <param name="fieldName"></param>
		/// <param name="srcField"></param>
		public void SetHeader(string includedAs, string fieldName, string srcField)
		{
			// lc first:
			var fieldNameLC = char.ToLower(fieldName[0]) + fieldName.Substring(1);
			var srcFieldLC = char.ToLower(srcField[0]) + srcField.Substring(1);

			var header = "{\"name\":\"" + includedAs + "\",\"field\":\"" + fieldNameLC + "\",\"src\":\"" + srcFieldLC + "\"";

			if (Parent.InclusionOutputIndex != -1)
			{
				header += ",\"on\":" + Parent.InclusionOutputIndex;
			}

			header += ",\"values\":[";

			_includeHeader = System.Text.Encoding.UTF8.GetBytes(header);
		}

		/// <summary>
		/// Gets a linked list of ID collectors from the pool, which match the set of IdFields that this node wants to collect.
		/// </summary>
		public IDCollector GetCollectors()
		{
			IDCollector first = null;
			IDCollector last = null;

			for (var i = 0; i < IdFields.Length; i++)
			{
				var collector = IdFields[i].RentCollector();

				if (i == 0)
				{
					first = collector;
					last = collector;
				}
				else
				{
					last.NextCollector = collector;
					last = collector;
				}
			}

			return first;
		}

		/// <summary>
		/// Add to tree
		/// </summary>
		/// <param name="field"></param>
		/// <param name="includeName">The original name of the include (in the include string).</param>
		public async ValueTask<InclusionNode> Add(ContentField field, string includeName)
		{
			if (UniqueChildNodes == null)
			{
				throw new System.Exception("Can't add to an include set after it has been baked.");
			}

			var name = field.VirtualInfo.FieldName;

			if (field.VirtualInfo.ValueGeneratorType != null)
			{
				// This is a functional include. It generates a value on the object itself, not after the set of objects has been outputted.

				if (Service == null || UniqueFunctionalIncludes.ContainsKey(name))
				{
					// The same thing was included 2+ times.
					return null;
				}

				var camelCaseName = Char.ToLowerInvariant(name[0]) + name.Substring(1);
				var functionalInclude = new FunctionalInclusionNode();
				UniqueFunctionalIncludes[name] = functionalInclude;
				functionalInclude.SetHeader(camelCaseName);
				var baseGenType = field.VirtualInfo.ValueGeneratorType;
				var typeToInstance = field.VirtualInfo.ValueGeneratorType.MakeGenericType(Service.ServicedType, Service.IdType);
				var valueGenerator = Activator.CreateInstance(typeToInstance); // as VirtualFieldValueGenerator<T, ID>;
				functionalInclude.ValueGenerator = valueGenerator;

				// Doesn't generate a node because nested functional includes don't make sense.
				return null;
			}

			var svc = field.VirtualInfo.Service;

			if (UniqueChildNodes.TryGetValue(name, out InclusionNode result))
			{
				return result;
			}

			// Get its service and add:
			result = new InclusionNode(svc.GetContentFields(), this);
			result.Service = svc;
			result.HostField = field;
			result.IncludeName = includeName;
			UniqueChildNodes[name] = result;

			if (field.VirtualInfo.IsList)
			{
				var mapInfo = await field.GetOptionalMappingService(RelativeTo);
				result.MappingService = mapInfo.Service;
				result.MappingTargetField = mapInfo.TargetField;
				result.MappingTargetFieldName = mapInfo.TargetFieldName;
			}

			return result;
		}

		/// <summary>
		/// Bakes the dictionary into a fast linear array of children.
		/// </summary>
		public void Bake(ref int outputIndex)
		{
			// Build the direct child node array:
			ChildNodes = new InclusionNode[UniqueChildNodes.Count];

			var i = 0;

			if (UniqueFunctionalIncludes.Count > 0)
			{
				FunctionalIncludes = new FunctionalInclusionNode[UniqueFunctionalIncludes.Count];

				foreach (var kvp in UniqueFunctionalIncludes)
				{
					FunctionalIncludes[i++] = kvp.Value;
				}
			}

			InclusionOutputIndex = outputIndex++; // output index is depth first.

			i = 0;
			foreach (var kvp in UniqueChildNodes)
			{
				ChildNodes[i++] = kvp.Value;
				kvp.Value.Bake(ref outputIndex);
			}

			UniqueChildNodes = null;
			UniqueFunctionalIncludes = null;

			if (HostField != null)
			{
				if (HostField.VirtualInfo.IsList)
				{
					SetHeader(IncludeName, HostField.VirtualInfo.FieldName);
				}
				else
				{
					// Regular virtual field:
					SetHeader(IncludeName, HostField.VirtualInfo.FieldName, HostField.VirtualInfo.IdSource.Name);
				}
			}
			
			// Next, we need to collect the unique set of fields from which IDs will be collected.
			// This identifies how many ID collectors are required, and what type/ field they'll collect from.
			// The ID collectors themselves form a stack style linked list to avoid 
			// allocation of anything other than the collectors themselves (which are also pooled).
			var idFields = new Dictionary<string, int>();
			var idFieldList = new List<ContentField>();

			for (var n = 0; n < ChildNodes.Length; n++)
			{
				var node = ChildNodes[n];

				// The ID source field is:
				var hostField = node.HostField;
				var idSource = hostField.VirtualInfo.IdSource;

				if (idSource == null && hostField.VirtualInfo.IdSourceField != null)
				{
					// This is where the host field is e.g. a global one (such as Tags). It knows the id source field (simply "Id" for tags)
					// but not the actual field, because they're global - they're on every type (multiple fields called Id).

					// So, relativeTo ideally exists at this point such that we can pre-resolve the actual Id field to use.
					// Note that it is null if the parent is a mixed content field.
					if (RelativeTo == null)
					{
						// Not supported yet! E.g. ContentUser on the content of a story (which could be a video, or a photo, or just text - etc).
						throw new PublicException("Unsupported include use case. If you would like it, please do ask!", "include_unsupported");
					}
					else
					{
						RelativeTo.NameMap.TryGetValue(hostField.VirtualInfo.IdSourceField.ToLower(), out idSource);
					}
				}

				if (idSource != null)
				{
					// Collecting an ID from this field.
					// Multiple things might want an ID from the same field (It happens with e.g. Tags + Categories)
					// so this one off dictionary makes sure IDs can be efficiently collected for all future requests.
					var fieldName = idSource.Name.ToLower();

					if (!idFields.TryGetValue(fieldName, out int index))
					{
						index = idFieldList.Count;
						idFields[fieldName] = index;
						idFieldList.Add(idSource);
					}

					node.CollectorIndex = index;
				}
			}

			// We've now got the unique set of fields to collect IDs from.
			// Essentially every object that goes by at this node of the include tree will have these fields collected into allocated (but pooled) unique ID collectors.

			// Note that here we just store a _description_ of the collectors - not actually create them.
			// That's because include nodes just describe the structure, rather than actually directly execute the inclusions.
			IdFields = idFieldList.ToArray();

			if (MappingService != null)
			{
				// Also need 1 ID collector on the mapping service as well (it collects the target IDs).

				// Note that it won't ever already be in there as the mapping type is something different.
				idFieldList.Add(MappingTargetField);

				// Resolve to concrete IDCollector<T> types for each of the ID fields.
				TypeIOEngine.GenerateIDCollectors(idFieldList.ToArray());
			}
			else
			{
				// Resolve to concrete IDCollector<T> types for each of the ID fields.
				TypeIOEngine.GenerateIDCollectors(IdFields);
			}
		}
	}

	/// <summary>
	/// A functional inclusion node. Including one of these triggers a custom function to run per object.
	/// </summary>
	public class FunctionalInclusionNode
	{

		/// <summary>
		/// The string ,"includeName":
		/// </summary>
		public byte[] _jsonPropertyHeader;

		/// <summary>
		/// Is a strong typed VirtualFieldValueGenerator. The type relates to the service for the current include node.
		/// For example, you ask for pages and include tags.primaryUrl. The root include node service is the pageservice, and the 1st child (tags) service is the tagService.
		/// The value generator for tags.primaryUrl is therefore a VirtualFieldValueGenerator for the tag type.
		/// </summary>
		public object ValueGenerator;

		/// <summary>
		/// Sets the header for this node.
		/// </summary>
		/// <param name="includedAs"></param>
		public void SetHeader(string includedAs)
		{
			_jsonPropertyHeader = System.Text.Encoding.UTF8.GetBytes(",\"" + includedAs + "\":");
		}

	}

}