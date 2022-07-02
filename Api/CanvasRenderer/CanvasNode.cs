using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Api.CanvasRenderer
{
	/// <summary>
	/// Particular node in the canvas tree.
	/// </summary>
	public class CanvasNode
	{
		/// <summary>
		/// Parses the given json into a canvas object.
		/// </summary>
		public static CanvasNode Load(string json)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject<CanvasNode>(json);
		}

		/// <summary>
		/// Create a canvas node with optional module name.
		/// </summary>
		/// <param name="module"></param>
		public CanvasNode(string module = null)
		{
			Module = module;
		}

		/// <summary>
		/// The data (attributes) for the node.
		/// </summary>
        public Dictionary<string, object> Data {get; set;}
		
		/// <summary>
		/// Any child nodes of a particular canvas node.
		/// </summary>
        public List<CanvasNode> Content {get; set;}
		
		/// <summary>
		/// The module to use.
		/// </summary>
        public string Module {get; set;}
		
		/// <summary>
		/// Get/ set data attributes on this node.
		/// </summary>
		public object this[string attrib]{
			get{
				if(Data == null)
				{
					return null;
				}
				Data.TryGetValue(attrib, out object value);
				return value;
			}
			set{
				Data[attrib] = value;
			}
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
		/// Converts canvas to JSON.
		/// </summary>
		public string ToJson()
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(this, jsonSettings);
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
		/// Sets an attribute of the given name to an optional value in a chainable way.
		/// </summary>
		public CanvasNode With(string attrib, object value = null){
			if(Data == null)
			{
				Data = new Dictionary<string, object>();
			}
			Data[attrib] = value;
			return this;
		}
		
	}
}
