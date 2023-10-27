using Api.SocketServerLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;


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
		/// A graph if there is one.
		/// </summary>
		public Graph Graph;

		/// <summary>
		/// The data (attributes) for the node as raw JSON tokens.
		/// </summary>
		public Dictionary<string, string> Data;

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
						writer.Write((byte)':');
						writer.WriteS(kvp.Value);
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
			},
			Formatting = Formatting.None
		};

		/// <summary>
		/// Sets an attribute of the given name to an optional value in a chainable way.
		/// </summary>
		public CanvasNode With(string attrib, object value = null){
			if(Data == null)
			{
				Data = new Dictionary<string, string>();
			}
			Data[attrib] = value == null ? null : Newtonsoft.Json.JsonConvert.SerializeObject(value, jsonSettings);
			return this;
		}

	}
}
