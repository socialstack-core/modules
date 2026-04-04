using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.SocketServerLibrary;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;

namespace Api.CanvasRenderer
{

	/// <summary>
	/// Particular node in the canvas tree.
	/// </summary>
	public class CanvasNode
	{
		/// <summary>
		/// Create a canvas node with optional module name.
		/// </summary>
		/// <param name="module"></param>
		public CanvasNode(string module = null)
		{
			Module = module;
		}

		/// <summary>
		/// The canvas that the node is a part of.
		/// </summary>
		public CanvasDetails Canvas;

		/// <summary>
		/// The source node. Usually an object.
		/// </summary>
		public JToken Source;

		/// <summary>
		/// A graph if there is one.
		/// </summary>
		public Graph Graph;

		/// <summary>
		/// The data (attributes) for the node as raw objects, usually strings.
		/// </summary>
		public Dictionary<string, CanvasAttribute> Data;

		/// <summary>
		/// The data store links, if there are any.
		/// </summary>
		public Dictionary<string, CanvasDataStoreLink> Links;

		/// <summary>
		/// The roots for the node, if any.
		/// </summary>
		public Dictionary<string, CanvasNode> Roots;

		/// <summary>
		/// Pointer (p) to an entry in a datamap (m).
		/// If it is a non-zero number, then this whole node is to be read from the datamap.
		/// If it is an object, then particular data values are to be read from the datamap.
		/// </summary>
		public uint Pointer;

		/// <summary>
		/// Pointer (p) to an entry in a datamap (m).
		/// If it is a non-zero number, then this whole node is to be read from the datamap.
		/// If it is an object, then particular data values are to be read from the datamap.
		/// </summary>
		public Dictionary<string, uint> Pointers;

		/// <summary>
		/// Any child nodes of a particular canvas node.
		/// </summary>
		public List<CanvasNode> Content;

		/// <summary>
		/// The module to use. Null if it is a string node.
		/// </summary>
		public string Module;

		/// <summary>
		/// Set if this is a text node.
		/// </summary>
		public string StringContent;

		/// <summary>
		/// Clears everything from this node.
		/// </summary>
		public CanvasNode Empty()
		{
			Content = null;
			Pointers = null;
			Module = null;
			StringContent = null;
			Roots = null;
			Links = null;
			Data = null;
			Graph = null;
			return this;
		}

		/// <summary>
		/// Data container to hold the object alongside a rendered json representation within 'Data' elements ,used for page layouts etc 
		/// </summary>
		public class CanvasAttribute
		{
			/// <summary>
			/// helper for creating 'Data' attribute values
			/// </summary>
			public CanvasAttribute()
			{

			}

			/// <summary>
			/// helper for creating 'Data' attribute values 
			/// </summary>
			/// <param name="value"></param>
			public CanvasAttribute(object value)
			{
				Value = value;
			}


			private object _value;
			private byte[] _json;

			/// <summary>
			/// Retrieve the bytes of the json representation of the object 
			/// </summary>
			public byte[] JsonBytes
			{
				get {
					return _json;
				}
			}

			/// <summary>
			/// Retrieve the raw/actual value
			/// </summary>
			public object Value
			{
				get
				{
					return _value;
				}
				set
				{
					_value = value;

					if (value is JsonString jsString)
					{
						var jsonString = jsString.ValueOf();

						_json = System.Text.Encoding.UTF8.GetBytes(
							jsonString == null ? "null" : jsonString
						);
					}
					else
					{
						_json = System.Text.Encoding.UTF8.GetBytes(
							value == null ? "null" : Newtonsoft.Json.JsonConvert.SerializeObject(value, jsonSettings)
						);
					}
				}
			}
		}


		/// <summary>
		/// Get or create a datamap entry for a field in a graph node.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="outputField"></param>
		/// <param name="dataMap"></param>
		/// <returns></returns>
		public static CanvasGeneratorMapEntry GetDataMapEntry(Executor node, string outputField, List<CanvasGeneratorMapEntry> dataMap)
		{
			for (var i = 0; i < dataMap.Count; i++)
			{
				var existing = dataMap[i];
				if (existing.GraphNode == node && existing.Field == outputField)
				{
					return existing;
				}
			}

			var cgm = new CanvasGeneratorMapEntry();
			cgm.GraphNode = node;
			cgm.Field = outputField;
			dataMap.Add(cgm);
			cgm.Id = (uint)dataMap.Count; // ID must be non-zero so we use index+1.
			node.AddDataMapOutput(cgm);
			return cgm;
		}

		/// <summary>
		/// Loads a node from the given newtonsoft object.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="node"></param>
		/// <param name="canvas"></param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException"></exception>
		public static async ValueTask<CanvasNode> LoadCanvasNode(
			Context context,
			JToken node,
			CanvasDetails canvas
		)
		{
			if (node == null)
			{
				return null;
			}

			var result = new CanvasNode();
			result.Canvas = canvas;
			result.Source = node;

			if (node.Type == JTokenType.String)
			{
				result.StringContent = node.Value<string>();
				return result;
			}

			if (node.Type == JTokenType.Array)
			{
				throw new NotSupportedException("Canvas with arrays are now only supported if the array is set as a content (c) value.");
			}

			// Here we only care about:
			// - t(ype)
			// - d(ata)
			// - s(trings)
			// - g(raphs)
			// - r(oots)
			// - c(ontent)

			// Type
			var type = node["t"];

			if (type != null)
			{
				result.Module = type.Value<string>();
			}

			// Data
			var data = node["d"] as JObject;

			if (data != null)
			{
				foreach (var kvp in data)
				{
					if (result.Data == null)
					{
						result.Data = new Dictionary<string, CanvasAttribute>();
					}

					CanvasAttribute val = new CanvasAttribute();

					if (kvp.Value.Type == JTokenType.Null)
					{
						val.Value = null;
					}
					else if (kvp.Value.Type == JTokenType.Boolean)
					{
						val.Value = kvp.Value.Value<bool>();
					}
					else if (kvp.Value.Type == JTokenType.Integer)
					{
						val.Value = kvp.Value.Value<long>();
					}
					else if (kvp.Value.Type == JTokenType.Float)
					{
						val.Value = kvp.Value.Value<double>();
					}
					else if (kvp.Value.Type == JTokenType.String)
					{
						val.Value = kvp.Value.Value<string>();
					}
					else if (kvp.Value.Type == JTokenType.Object || kvp.Value.Type == JTokenType.Array)
					{
						val.Value = kvp.Value;
					}
					else
					{
						val.Value = kvp.Value.ToObject<object>();
					}

					result.Data[kvp.Key] = val;
				}
			}

			// Strings
			var str = node["s"];

			if (str != null)
			{
				result.StringContent = str.Value<string>();
			}

			// Graphs
			var graphData = node["g"];

			if (graphData != null)
			{
				if (canvas == null || canvas.GraphNodeLoader == null)
				{
					throw new Exception("Discovered a graph in a canvas that does not support them. This is typically because a graph is present in something like a template canvas.");
				}

				// Found a graph. Load it using the canvas-wide graph node loader.
				var graph = new Graph(graphData, canvas.GraphNodeLoader);

				// If the root node is a component then this canvas node morphs in to that component.
				var comp = graph.Root as Component;
				if (comp != null)
				{
					// Inline it now.
					foreach (var kvp in comp.ConstantData)
					{
						if (kvp.Key == "componentType")
						{
							result.Module = kvp.Value.ToString();
						}
						else
						{
							if (result.Data == null)
							{
								result.Data = new Dictionary<string, CanvasAttribute>();
							}

							string val;

							if (kvp.Value.Type == JTokenType.Null)
							{
								val = null;
							}
							else if (kvp.Value.Type == JTokenType.Boolean)
							{
								val = kvp.Value.Value<bool>() ? "true" : "false";
							}
							else if (kvp.Value.Type == JTokenType.String)
							{
								val = kvp.Value.Value<string>();
							}
							else
							{
								val = kvp.Value.ToString();
							}

							result.Data[kvp.Key] = new CanvasAttribute(val);
						}
					}

					// Each link (non-constant data) becomes a datamap pointer.
					foreach (var kvp in comp.Links)
					{
						if (result.Pointers == null)
						{
							result.Pointers = new Dictionary<string, uint>();
						}

						if (canvas == null || canvas.DataMap == null)
						{
							throw new Exception("Non-constant data links are in use but not supported by this canvas. " +
								"This is usually because they are present in e.g. templates.");
						}

						var cdm = GetDataMapEntry(kvp.Value.SourceNode.AddedAs, kvp.Value.Field, canvas.DataMap);
						result.Pointers[kvp.Key] = cdm.Id;
					}
				}
				else
				{
					result.Graph = graph;
				}
			}

			// Links
			var links = node["l"] as JObject;

			if (links != null)
			{
				foreach (var kvp in links)
				{
					if (result.Links == null)
					{
						result.Links = new Dictionary<string, CanvasDataStoreLink>();
					}

					var jsonNode = kvp.Value;
					var fieldJson = jsonNode["field"];
					var writeJson = jsonNode["write"];
					var primaryJson = jsonNode["primary"];

					result.Links[kvp.Key] = new CanvasDataStoreLink(
						fieldJson == null ? null : fieldJson.Value<string>(),
						writeJson == null ? false : writeJson.Value<bool>(),
						primaryJson == null ? false : primaryJson.Value<bool>()
					);
				}
			}

			// Roots
			var roots = node["r"] as JObject;

			if (roots != null)
			{
				foreach (var kvp in roots)
				{
					if (result.Roots == null)
					{
						result.Roots = new Dictionary<string, CanvasNode>();
					}

					result.Roots[kvp.Key] = await LoadCanvasNode(context, kvp.Value, canvas);
				}
			}

			// Content
			var content = node["c"];

			if (content != null)
			{
				// Content can be: an array an object or a string.
				var array = content as JArray;

				if (array != null)
				{
					for (var i = 0; i < array.Count; i++)
					{
						if (result.Content == null)
						{
							result.Content = new List<CanvasNode>();
						}

						var child = await LoadCanvasNode(context, array[i], canvas);

						if (child != null)
						{
							result.Content.Add(child);
						}
					}
				}
				else
				{
					// Either a string or object.
					var child = await LoadCanvasNode(context, content, canvas);

					if (result.Content == null)
					{
						result.Content = new List<CanvasNode>();
					}

					if (child != null)
					{
						result.Content.Add(child);
					}
				}
			}

			// Modules may want to transform this node (such as templates)
			result = await Events.Page.TransformCanvasNode.Dispatch(context, result);

			return result;
		}


		/// <summary>
		/// Convert html into a canvas type object (usually just used for migrations)
		/// </summary>
		/// <param name="html"></param>
		/// <returns></returns>
		public static CanvasNode HtmlToCanvas(string html)
		{
			if (string.IsNullOrWhiteSpace(html))
			{
				return new CanvasNode();
			}

			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			return ConvertHtmlNode(doc.DocumentNode, true);
		}

		/// <summary>
		/// Convert an html node and its children into canvas node
		/// </summary>
		/// <param name="node"></param>
		/// <param name="stripWrapper"></param>
		/// <returns></returns>
		public static CanvasNode ConvertHtmlNode(HtmlNode node, bool stripWrapper = false)
		{
			if (node == null || (node.NodeType != HtmlNodeType.Text && node.NodeType != HtmlNodeType.Element && node.NodeType != HtmlNodeType.Document))
			{
				return new CanvasNode();
			}

			CanvasNode canvasNode = null;

			if (node.NodeType == HtmlNodeType.Document)
			{
				// if we only have 1 top level node then optionally strip it and just use the contents (redundant <div>/<p> etc)
				if (stripWrapper && node.HasChildNodes && node.ChildNodes.Count == 1)
				{
					return ConvertHtmlNode(node.ChildNodes[0]);
				}

				// outer wrapper 
				canvasNode = new CanvasNode();
			}
			else if (node.NodeType == HtmlNodeType.Text)
			{
				return new CanvasNode() { StringContent = HttpUtility.HtmlDecode(node.InnerText) };
			}

			else if (node.Name.ToLower() == "iframe" && node.Attributes.Contains("src"))
			{
				var src = node.GetAttributeValue("src", "");
				if (src.StartsWith("https://www.youtube.com/embed/"))
				{
					canvasNode = new CanvasNode("UI/Video").With("fileRef", "youtube:" + src.Substring(30));
				}
				else if (src.StartsWith("https://fast.wistia.net/embed/iframe/"))
				{
					canvasNode = new CanvasNode("UI/Video").With("fileRef", "wistia:" + src.Substring(37));
				}
				else
				{
					canvasNode = new CanvasNode(node.Name).With("src", src);
				}
			}
			else
			{
				canvasNode = new CanvasNode(node.Name);
			}


			//var StringContent = string.Join(" ", node.ChildNodes
			//    .Where(cn => cn.NodeType == HtmlNodeType.Text)
			//    .Select(cn => cn.InnerText.Trim()));

			//if (!string.IsNullOrWhiteSpace(StringContent))
			//{
			//    canvasNode.AppendChild(HttpUtility.HtmlDecode(StringContent));
			//}

			foreach (var child in node.ChildNodes)
			{
				//if (child.NodeType == HtmlNodeType.Element)
				//{
				var childCanvas = ConvertHtmlNode(child);
				if (childCanvas != null)
					canvasNode.AppendChild(childCanvas);
				//}
			}

			return canvasNode;
		}


		/// <summary>
		/// Converts canvas to JSON.
		/// </summary>
		/// <param name="leaveOpen">If true, does not write the closing curly bracket</param>
		public string ToJson(bool leaveOpen = false)
		{
			var writer = Writer.GetPooled();
			writer.Start(null);
			ToJson(writer, leaveOpen);
			var result = writer.ToUTF8String();
			writer.Release();
			return result;
		}

		/// <summary>
		/// Converts canvas to JSON as bytes.
		/// </summary>
		/// <param name="leaveOpen">If true, does not write the closing curly bracket</param>
		public byte[] ToJsonBytes(bool leaveOpen = false)
		{
			var writer = Writer.GetPooled();
			writer.Start(null);
			ToJson(writer, leaveOpen);
			var result = writer.AllocatedResult();
			writer.Release();
			return result;
		}

		/// <summary>
		/// Converts canvas node to JSON.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="leaveOpen">If true, does not write the closing curly bracket</param>
		public void ToJson(Writer writer, bool leaveOpen = false)
		{
			if (Pointer != 0)
			{
				// Numeric pointer - only thing that is permitted in this node is the pointer itself.
				writer.WriteASCII("{\"p\":");
				writer.WriteS(Pointer);

				if (!leaveOpen)
				{
					writer.Write((byte)'}');
				}

				return;
			}

			if (Graph != null)
			{
				writer.WriteASCII("{\"g\":");
				Graph.ToJson(writer);
			}
			else if (StringContent != null)
			{
				writer.WriteASCII("{\"s\":");
				writer.WriteEscaped(StringContent);
			}
			else
			{
				writer.WriteASCII("{\"t\":");
				writer.WriteEscaped(Module);
			}

			if (Roots != null && Roots.Count > 0)
			{
				writer.WriteASCII(",\"r\":{");

				var first = true;

				foreach (var kvp in Roots)
				{
					if (first)
					{
						first = false;
					}
					else
					{
						writer.Write((byte)',');
					}
					writer.WriteEscaped(kvp.Key);
					if (kvp.Value == null)
					{
						writer.WriteASCII(":null");
					}
					else
					{
						writer.Write((byte)':');
						kvp.Value.ToJson(writer);
					}
				}

				writer.Write((byte)'}');
			}

			if (Links != null && Links.Count > 0)
			{
				writer.WriteASCII(",\"l\":{");

				var first = true;

				foreach (var kvp in Links)
				{
					if (first)
					{
						first = false;
					}
					else
					{
						writer.Write((byte)',');
					}
					writer.WriteEscaped(kvp.Key);
					writer.Write((byte)':');
					writer.WriteS(kvp.Value.JsonString);
				}

				writer.Write((byte)'}');
			}

			if (Data != null && Data.Count > 0)
			{
				writer.WriteASCII(",\"d\":{");

				var first = true;

				foreach (var kvp in Data)
				{
					if (first)
					{
						first = false;
					}
					else
					{
						writer.Write((byte)',');
					}
					
					writer.WriteEscaped(kvp.Key);
					if (kvp.Value == null)
					{
						writer.WriteASCII(":null");
					}
					else
					{
						var bytes = kvp.Value.JsonBytes;

						if (bytes == null)
						{
							writer.WriteASCII(":null");
						}
						else
						{
							writer.Write((byte)':');
							writer.Write(bytes, 0, bytes.Length);
						}
					}
				}

				writer.Write((byte)'}');
			}

			var contentCount = Content == null ? 0 : Content.Count;

			if (contentCount == 1)
			{
				writer.WriteASCII(",\"c\":");
				Content[0].ToJson(writer);
			}
			else if (contentCount > 0)
			{
				writer.WriteASCII(",\"c\":[");

				for (var i = 0; i < Content.Count; i++)
				{
					if (i != 0)
					{
						writer.Write((byte)',');
					}
					Content[i].ToJson(writer);
				}

				writer.Write((byte)']');
			}

			// i and ti (ID and TemplateID) are not necessary.

			if (Pointers != null && Pointers.Count > 0)
			{
				writer.WriteASCII(",\"p\":{");

				var first = true;

				foreach (var kvp in Pointers)
				{
					if (first)
					{
						first = false;
					}
					else
					{
						writer.Write((byte)',');
					}
					writer.WriteEscaped(kvp.Key);
					writer.Write((byte)':');
					writer.WriteS(kvp.Value);
				}

				writer.Write((byte)'}');
			}

			if (!leaveOpen)
			{
				writer.Write((byte)'}');
			}
		}

		/// <summary>
		/// Adds a root with the given name and content, returning the original node.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="content"></param>
		/// <returns></returns>
		public CanvasNode AddRoot(string name, CanvasNode content)
		{
			if (Roots == null)
			{
				Roots = new Dictionary<string, CanvasNode>();
			}

			Roots[name] = content;
			return this;
		}

		/// <summary>
		/// Appends string content as a child of this node.
		/// </summary>
		/// <param name="stringContent"></param>
		/// <returns></returns>
		public CanvasNode AppendChild(string stringContent)
		{
			var newNode = new CanvasNode() { StringContent = stringContent };
			return AppendChild(newNode);
		}

		/// <summary>
		/// Chainable append child.
		/// </summary>
		/// <param name="child"></param>
		/// <returns></returns>
		public CanvasNode AppendChild(CanvasNode child)
		{
			if (Content == null)
			{
				Content = new List<CanvasNode>();
			}
			Content.Add(child);
			return this;
		}

		/// <summary>
		/// Json serialization settings for canvases
		/// </summary>
		private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new CamelCaseNamingStrategy()
				{
					ProcessDictionaryKeys = true,
					OverrideSpecifiedNames = false
				}
			},
			Formatting = Formatting.None
		};

		/// <summary>
		/// Sets an attribute of the given name to an optional value in a chainable way.
		/// </summary>
		public CanvasNode With(string attrib, object value = null)
		{
			if (Data == null)
			{
				Data = new Dictionary<string, CanvasAttribute>();
			}
			Data[attrib] = new CanvasAttribute(value);
			return this;
		}

		/// <summary>
		/// Sets an attribute of the given name to being the primary object in a chainable way.
		/// </summary>
		public CanvasNode WithPrimaryLink(string attrib)
		{
			if (Links == null)
			{
				Links = new Dictionary<string, CanvasDataStoreLink>();
			}

			Links[attrib] = new CanvasDataStoreLink(null, false, true);
			return this;
		}

		/// <summary>
		/// Sets an attribute of the given name to being the primary object in a chainable way.
		/// </summary>
		public CanvasNode WithLink(string attrib, string field, bool isWriteable)
		{
			if (Links == null)
			{
				Links = new Dictionary<string, CanvasDataStoreLink>();
			}

			Links[attrib] = new CanvasDataStoreLink(field, isWriteable, false);
			return this;
		}


		/// <summary>
		/// Search for a module within a canvas node and its children
		/// </summary>
		/// <param name="module"></param>
		/// <returns></returns>
		public CanvasNode Find(string module)
		{
			if (Module == module)
			{
				return this;
			}

			if (Content == null)
			{
				return null;
			}

			foreach (var child in Content)
			{
				if (child == null)
				{
					continue;
				}

				var result = child.Find(module);

				if (result != null)
				{
					return result;
				}
			}

			return null;
		}
	}

	/// <summary>
	/// A data store link.
	/// </summary>
	public struct CanvasDataStoreLink
	{
		/// <summary>
		/// True if it's the write direction. A function is passed to the prop which takes 1 arg, the value to write.
		/// </summary>
		public bool Write;

		/// <summary>
		/// The field.
		/// </summary>
		public string Field;

		/// <summary>
		/// True if write/field are ignored and the primary object is provided to the prop.
		/// </summary>
		public bool Primary;

		/// <summary>
		/// A preconstructed json string.
		/// </summary>
		public readonly string JsonString;

		/// <summary>
		/// Create a new link.
		/// </summary>
		/// <param name="field"></param>
		/// <param name="write"></param>
		/// <param name="primary"></param>
		public CanvasDataStoreLink(string field, bool write, bool primary)
		{
			JsonString = "{\"write\": " + (write ? "true" : "false") + ",\"primary\": " + (primary ? "true" : "false") + ",\"field\":\"" + field + "\"}";
		}
	}

	/// <summary>
	/// Details for the canvas being loaded, if any.
	/// </summary>
	public partial class CanvasDetails
	{
		/// <summary>
		/// A graph node loader if one is present.
		/// </summary>
		public NodeLoader GraphNodeLoader;

		/// <summary>
		/// The data map if one is present.
		/// </summary>
		public List<CanvasGeneratorMapEntry> DataMap;
	}
}
