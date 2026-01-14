
using Api.Contexts;
using Api.SocketServerLibrary;
using System;
using System.Collections.Generic;
using System.IO;
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
		public void Parse()
		{
			RootInclude = new InclusionNode(RelativeTo, null);
			RootInclude.Service = RelativeTo.Service;

			int start = 0;
			var comma = IncludeString.IndexOf(',');
			
			while (comma != -1)
			{
				var fieldName = IncludeString.Substring(start, comma - start);

				// Add the given field:
				Add(fieldName);

				// Next comma:
				start = comma + 1;
				comma = IncludeString.IndexOf(',', start);
			}

			Add(start == 0 ? IncludeString : IncludeString.Substring(start));

			// Bake the root:
			int outputIndex = -1;
			RootInclude.Bake(ref outputIndex);

			if (RootInclude.SecondaryRoot != null)
			{
				var secondary = RootInclude.SecondaryRoot;

				foreach (var kvp in secondary.UniqueChildNodes)
				{
					outputIndex = -1;
					kvp.Value.Bake(ref outputIndex);
				}
			}
		}

		/// <summary>
		/// Gets the root inclusion node for a secondary set by its lowercase name.
		/// </summary>
		/// <param name="lcName"></param>
		/// <returns></returns>
		public InclusionNode GetSecondaryNode(string lcName)
		{
			var secondary = RootInclude.SecondaryRoot;

			if (secondary == null)
			{
				return null;
			}

			secondary.UniqueChildNodes.TryGetValue(lcName, out InclusionNode node);
			return node;
		}

		/// <summary>
		/// Root inclusion.
		/// </summary>
		public InclusionNode RootInclude;

		/// <summary>
		/// Adds a field to the set. It can contain a * (or be just "*"), but not after a mixed content type field.
		/// </summary>
		/// <param name="rootRelativeFieldName"></param>
		private void Add(string rootRelativeFieldName)
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

				// Trim the part:
				currentName = currentName.Trim();

				if (current.IsSecondaryRoot && current.RelativeTo != null)
				{
					// Only one valid source for currentName.

					if (
						!current.RelativeTo.SecondaryResultNameMap.TryGetValue(currentName, out ContentField secondaryField))
					{
						throw new PublicException("Your request tried to use '" + currentName + "' in include '" + rootRelativeFieldName + "' but it doesn't exist", "include_no_exist");
					}

					// Add field:
					current = current.Add(secondaryField, rootRelativeFieldName);
					continue;
				}

				if (current.TypeSource != null)
				{
					// An include on a dynamic include.
					// Append it to a string, with all remaining pieces.
					var thisInclude = "";

					for (var n = i; n < pieces.Length; n++)
					{
						if (n != i)
						{
							thisInclude += '.';
						}
						thisInclude += pieces[n];
					}

					if (current.DynamicChildIncludes == null)
					{
						current.DynamicChildIncludes = thisInclude;
					}
					else
					{
						current.DynamicChildIncludes += "," + thisInclude;
					}
					return;
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
					if (current.RelativeTo != null)
					{
						foreach (var field in current.RelativeTo.VirtualList)
						{
							current.Add(field, rootRelativeFieldName);
						}

						if (!current.RelativeTo.IsContentType)
						{
							// ListAs fields are not present on non-content types (because Mappings doesn't exist).
							break;
						}
					}

					// Add every ListAs field except for the explicit ones where this is not an implicit type.
					foreach (var kvp in ContentFields.GlobalVirtualFields)
					{
						var virtInfo = kvp.Value.VirtualInfo;

						if (virtInfo != null && virtInfo.IsExplicit)
						{
							// Check if this is one of the implicit types.
							if (current.RelativeTo == null || !virtInfo.IsImplicitFor(current.RelativeTo.InstanceType))
							{
								// Skip!
								continue;
							}
						}

						current.Add(kvp.Value, rootRelativeFieldName);
					}

					// Can't continue (at least, not in this version) because 'current' is now a set of nodes.
					break;
				}

				// Is it a global field?
				var globalPermitted = true;

				if (current.RelativeTo != null && !current.RelativeTo.IsContentType)
				{
					// Non-content types do not support Mappings and thus cannot use these global list fields.
					globalPermitted = false;
				}

				if (globalPermitted && ContentFields.GlobalVirtualFields.TryGetValue(currentName, out ContentField globalField))
				{
					// Yes! it's a global - these ones are available on all types. They're usually lists, like tags or categories.
					// Service and type are required for these.
					current = current.Add(globalField, rootRelativeFieldName);
					continue;
				}

				// How about the special 'secondary' sub-root node?
				if (i == 0 && currentName == "secondary" && current.RelativeTo != null)
				{
					current = current.AddSecondary();
					continue;
				}

				// It must be a local virtual field, otherwise it doesn't exist:
				if (
					current.RelativeTo == null || 
					!current.RelativeTo.LocalVirtualNameMap.TryGetValue(currentName, out ContentField localField))
				{
					throw new PublicException("Your request tried to use '" + currentName + "' in include '" + rootRelativeFieldName +  "' but it doesn't exist", "include_no_exist");
				}

				// Add local field:
				current = current.Add(localField, rootRelativeFieldName);
			}

		}
		
	}

	/// <summary>
	/// A collectible include field.
	/// </summary>
	public struct CollectibleIncludeField
	{
		/// <summary>
		/// The ID field itself.
		/// </summary>
		public ContentField IdField;

		/// <summary>
		/// ,"includeFieldName":
		/// </summary>
		public string JsonFieldHeading;

		private bool IsDynamicInclude;

		/// <summary>
		/// Creates a new collectible field.
		/// </summary>
		/// <param name="idField"></param>
		/// <param name="fieldName">The original virtual field name.</param>
		public CollectibleIncludeField(ContentField idField, string fieldName)
		{
			IdField = idField;
			var lcFieldName = char.ToLower(fieldName[0]) + fieldName.Substring(1);
			JsonFieldHeading = ",\"" + lcFieldName + "\":";
			IsDynamicInclude = idField.VirtualInfo != null && idField.VirtualInfo.DynamicTypeField != null;
		}

		/// <summary>
		/// Rent an ID collector for this field.
		/// </summary>
		/// <returns></returns>
		public IDCollector RentCollector()
		{
			if (IsDynamicInclude)
			{
				var multiCollector = IdField.RentMultiCollector();
				multiCollector.JsonFieldHeading = JsonFieldHeading;
				return multiCollector;
			}
			var collector = IdField.RentCollector();
			collector.JsonFieldHeading = JsonFieldHeading;
			return collector;
		}
	}

	/// <summary>
	/// A particular inclusion node in the tree of nodes.
	/// </summary>
	public class InclusionNode
	{
		/// <summary>
		/// ,"includes":[ 
		/// </summary>
		private static readonly byte[] IncludesHeader = new byte[] {
		(byte)',', (byte)'"', (byte)'i', (byte)'n', (byte)'c', (byte)'l', (byte)'u', (byte)'d', (byte)'e', (byte)'s', (byte)'"', (byte)':', (byte)'['
	};

		private static readonly byte[] IncludesFooter = new byte[] { (byte)']', (byte)'}' };

		/// <summary>
		/// End of include block. ]}.
		/// </summary>
		private static readonly byte[] IncludesValueFooter = new byte[] { (byte)']', (byte)'}' };

		/// <summary>
		/// End of dynamic include block. }}.
		/// </summary>
		private static readonly byte[] IncludesDynamicValueFooter = new byte[] { (byte)'}', (byte)'}' };

		/// <summary>
		/// The field that sources data for this inclusion node.
		/// </summary>
		public ContentField HostField;

		/// <summary>
		/// The first character lowercased field name of the inclusion. Not to be confused with the inclusion name.
		/// The inclusion name can be nested e.g. "tags.creatorUser" whilst the field name is just "creatorUser" or "tags".
		/// </summary>
		public string FieldName;

		/// <summary>
		/// The service to use to resolve the actual value of this node.
		/// </summary>
		public AutoService Service;

		/// <summary>
		/// A field from which the type to use comes.
		/// </summary>
		public ContentField TypeSource;

		/// <summary>
		/// For includes which are children of a dynamic include node.
		/// This string is evaluated as a new include set when the actual target service is known.
		/// Because the include set comes from the includes cache, it incurs only a very little penalty.
		/// </summary>
		public string DynamicChildIncludes;

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
		public CollectibleIncludeField[] IdFields;

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
		/// The secondary result set root if there is one.
		/// </summary>
		public InclusionNode SecondaryRoot;

		/// <summary>
		/// The index of this inclusion in the output inclusion array.
		/// </summary>
		public int InclusionOutputIndex = -1;

		/// <summary>
		/// E.g. TagId - the field in a mapping row that represents the target object. This must be collected as well.
		/// </summary>
		public ContentField MappingTargetField;

		/// <summary>
		/// True if this node is the secondary result set root. Only secondary result sets are valid includes on it.
		/// </summary>
		public bool IsSecondaryRoot;

		/// <summary>
		/// Create a new node
		/// </summary>
		/// <param name="relativeTo"></param>
		/// <param name="parent"></param>
		/// <param name="isSecondaryRoot"></param>
		public InclusionNode(ContentFields relativeTo, InclusionNode parent, bool isSecondaryRoot = false)
		{
			RelativeTo = relativeTo;
			Parent = parent;
			IsSecondaryRoot = isSecondaryRoot;
		}

		/// <summary>
		/// Used to execute includes.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="targetStream"></param>
		/// <param name="writer"></param>
		/// <param name="firstCollector"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public async ValueTask ExecuteIncludes(Context context, Stream targetStream, Writer writer, IDCollector firstCollector, ContextFlags flags)
		{
			flags |= ContextFlags.IsIncluded;

			// Now all IDs that are needed have been collected,
			// go through the inclusions and perform the include.
			var includesToExecute = ChildNodes;

			for (var i = 0; i < includesToExecute.Length; i++)
			{
				var toExecute = includesToExecute[i];

				if (toExecute.InclusionOutputIndex != 0)
				{
					// Comma between includes. Exists for all nodes except the very first include (output index 0).
					writer.Write((byte)',');
				}

				// Write the inclusion node header:
				var h = toExecute.IncludeHeader;
				writer.Write(h, 0, h.Length);

				// Get ID collector:
				var collector = firstCollector;
				var curIndex = 0;

				// A linked list is by far the best structure here - the set is usually tiny and it avoids allocating.
				while (curIndex < toExecute.CollectorIndex)
				{
					curIndex++;
					collector = collector.NextCollector;
				}

				// Spawn child collectors now, if we need any.
				var childCollectors = toExecute.GetCollectors();

				if (toExecute.TypeSource != null)
				{
					// The collector in this case is a MultiIdCollector.
					// Ask each service in it (which can be none) to output a JSON list.
					var multiCollector = collector as MultiIDCollector;

					if (multiCollector != null)
					{
						for (var n = 0; n < multiCollector.CollectorFill; n++)
						{
							var cbt = multiCollector.CollectorsByType[n];
							if (n == 0)
							{
								writer.Write((byte)'\"');
							}
							else
							{
								writer.WriteASCII(",\"");
							}
							writer.WriteASCII(cbt.Service.EntityName);

							writer.WriteASCII("\":{\"results\":[");

							// Load the sub-include set:
							if (toExecute.DynamicChildIncludes != null)
							{
								var childIncludeSet = cbt.Service.GetContentFields().GetIncludeSet(toExecute.DynamicChildIncludes);

								if (childIncludeSet != null)
								{
									// We've got some includes to add.

									// First we need to obtain ID collectors, and then collect the IDs.
									var childRoot = childIncludeSet.RootInclude;

									var firstChildCollector = childRoot.GetCollectors();

									await cbt.Service.OutputJsonList(
										context,
										firstChildCollector,
										cbt.Collector,
										writer, flags,
										childIncludeSet.RootInclude.FunctionalIncludes
									);

									writer.Write((byte)']');
									// Write the includes header, then write out the data so far.
									writer.Write(IncludesHeader, 0, 13);

									if (childRoot.ChildNodes != null && childRoot.ChildNodes.Length > 0)
									{
										// NB: This will release the child collectors for us.
										await childRoot.ExecuteIncludes(context, targetStream, writer, firstChildCollector, flags);
									}

									writer.Write(IncludesFooter, 0, 2);
								}
								else
								{
									await cbt.Service.OutputJsonList(context, null, cbt.Collector, writer, flags, null);
									writer.Write(IncludesValueFooter, 0, 2);
								}
							}
							else
							{
								await cbt.Service.OutputJsonList(context, null, cbt.Collector, writer, flags, null);
								writer.Write(IncludesValueFooter, 0, 2);
							}
						}
					}

					// End of this include.
					writer.Write(IncludesDynamicValueFooter, 0, 2);

				}
				else
				{
					// Directly use IDs in collector with the service.
					await toExecute.Service.OutputJsonList(context, childCollectors, collector, writer, flags, toExecute.FunctionalIncludes);

					// End of this include.
					writer.Write(IncludesValueFooter, 0, 2);
				}

				// Did it have any child nodes? If so, execute those as well.
				// Above we will have collected the IDs that the children need.
				if (toExecute.ChildNodes != null && toExecute.ChildNodes.Length > 0)
				{
					// NB: This will release the child collectors for us.
					await toExecute.ExecuteIncludes(context, targetStream, writer, childCollectors, flags);
				}
			}

			// Release the collectors:
			var current = firstCollector;

			while (current != null)
			{
				var next = current.NextCollector;
				current.Release();
				current = next;
			}
		}

		/// <summary>
		/// Sets this include node as a ListAs with the given header info.
		/// </summary>
		/// <param name="includedAs">The raw text in the include string that this include node came from.</param>
		/// <param name="fieldName"></param>
		public void SetHeader(string includedAs, string fieldName)
		{
			if (string.IsNullOrEmpty(fieldName))
			{
				throw new Exception("Can't ListAs() a blank field name. It's required.");
			}

			var header = "{\"name\":\"" + includedAs + "\",\"field\":\"" + fieldName + "\"";

			if (Parent.InclusionOutputIndex != -1)
			{
				header += ",\"on\":" + Parent.InclusionOutputIndex;
			}

			if (TypeSource != null)
			{
				// Multi include
				header += ",\"values\":{";
			}
			else
			{
				header += ",\"values\":[";
			}

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
		/// Adds the secondary root node set.
		/// </summary>
		/// <returns></returns>
		public InclusionNode AddSecondary()
		{
			if (UniqueChildNodes == null)
			{
				throw new System.Exception("Can't add to an include set after it has been baked.");
			}

			if (SecondaryRoot != null)
			{
				return SecondaryRoot;
			}

			var result = new InclusionNode(RelativeTo, this, true);
			result.IncludeName = "secondary";
			SecondaryRoot = result;
			return result;
		}

		/// <summary>
		/// Add to tree
		/// </summary>
		/// <param name="field"></param>
		/// <param name="includeName">The original name of the include (in the include string).</param>
		public InclusionNode Add(ContentField field, string includeName)
		{
			if (UniqueChildNodes == null)
			{
				throw new System.Exception("Can't add to an include set after it has been baked.");
			}

			if (field.SecondaryInfo != null)
			{
				// This is a secondary result set. It is not necessarily even a content type.

				var secName = field.SecondaryInfo.FieldName;

				if (UniqueChildNodes.TryGetValue(secName, out InclusionNode secResult))
				{
					return secResult;
				}

				var camelCaseName = Char.ToLowerInvariant(secName[0]) + secName.Substring(1);

				if (field.SecondaryInfo.Service != null)
				{
					// Regular include node from here.
					secResult = new InclusionNode(field.SecondaryInfo.Service.GetContentFields(), this);
					secResult.Service = field.SecondaryInfo.Service;
				}
				else
				{
					// Non-content type secondary result set. It only has local includes, nothing else.
					var cf = new ContentFields(field.SecondaryInfo.Type, false);
					secResult = new InclusionNode(cf, this);
				}

				secResult.HostField = field;
				secResult.IncludeName = includeName;
				UniqueChildNodes[secName] = secResult;
				return secResult;
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
				var baseValueGen = valueGenerator as VirtualFieldValueGenerator;
				baseValueGen.SetService(Service);
				functionalInclude.ValueGenerator = baseValueGen;

				// Doesn't generate a node because nested functional includes don't make sense.
				return null;
			}

			var svc = field.VirtualInfo.Service;

			if (UniqueChildNodes.TryGetValue(name, out InclusionNode result))
			{
				return result;
			}

			if (field.VirtualInfo.DynamicTypeField != null)
			{
				// Dynamic include
				result = new InclusionNode(null, this);
				result.TypeSource = field.VirtualInfo.DynamicTypeField;
			}
			else
			{
				// Regular include
				result = new InclusionNode(svc.GetContentFields(), this);
				result.Service = svc;
			}
			
			result.HostField = field;
			result.IncludeName = includeName;
			UniqueChildNodes[name] = result;
			
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
				var fieldName = HostField.VirtualInfo != null ? HostField.VirtualInfo.FieldName : HostField.SecondaryInfo.FieldName;
				FieldName = char.ToLower(fieldName[0]) + fieldName.Substring(1);
				SetHeader(IncludeName, FieldName);
			}

			var idFieldList = new List<CollectibleIncludeField>();

			for (var n = 0; n < ChildNodes.Length; n++)
			{
				var node = ChildNodes[n];

				// The ID source field is:
				var hostField = node.HostField;

				if (hostField.VirtualInfo.IsList)
				{
					// List fields.

					// The host field itself is the thing to emit and collect.
					node.CollectorIndex = idFieldList.Count;
					idFieldList.Add(new CollectibleIncludeField(hostField, hostField.VirtualInfo.FieldName));
					continue;
				}
				
				if (hostField.VirtualInfo.DynamicTypeField != null)
				{
					// Dynamic includes.
					// The host field itself is the thing to emit and collect.
					node.CollectorIndex = idFieldList.Count;
					idFieldList.Add(new CollectibleIncludeField(hostField, hostField.VirtualInfo.FieldName));
					continue;
				}

				var idSource = hostField.VirtualInfo.IdSource;

				if (idSource == null)
				{
					throw new PublicException(
						"Unable to use an include '" + node.IncludeName + "' as it appears to be configured incorrectly (missing an ID source)", 
						"include/invalid"
					);
				}

				// Collecting an ID from this field.
				// Multiple things might want an ID from the same field (It happens with e.g. Tags + Categories)
				// so this one off dictionary makes sure IDs can be efficiently collected for all future requests.
				var fieldName = idSource.Name.ToLower();

				node.CollectorIndex = idFieldList.Count;
				idFieldList.Add(new CollectibleIncludeField(idSource, hostField.VirtualInfo.FieldName));
			}

			// We've now got the unique set of fields to collect IDs from.
			// Essentially every object that goes by at this node of the include tree will have these fields collected into allocated (but pooled) unique ID collectors.

			// Note that here we just store a _description_ of the collectors - not actually create them.
			// That's because include nodes just describe the structure, rather than actually directly execute the inclusions.
			IdFields = idFieldList.ToArray();
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
		public VirtualFieldValueGenerator ValueGenerator;

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