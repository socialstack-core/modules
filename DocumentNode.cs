using Api.Contexts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Api.Pages
{
	/// <summary>
	/// A node in the HTML of a page being generated.
	/// </summary>
	public partial class DocumentNode
	{
		/// <summary>
		/// The HTML node name, in lowercase.
		/// </summary>
		public string NodeName;
		
		/// <summary>
		/// The attributes for this node.
		/// </summary>
		public Dictionary<string, string> Attributes = new Dictionary<string, string>();
		
		/// <summary>
		/// The child nodes.
		/// </summary>
		public List<DocumentNode> ChildNodes;

		/// <summary>
		/// True if you want to supress the closing / if childNodes is null. Only applicable to !doctype.
		/// </summary>
		public bool SupressSelfClose;
		
		/// <summary>
		/// 
		/// </summary>
		public DocumentNode(){
			ChildNodes = new List<DocumentNode>();
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="nodeName"></param>
		/// <param name="selfClosing"></param>
		public DocumentNode(string nodeName, bool selfClosing = false){
			NodeName = nodeName;
			if(!selfClosing) // e.g. <link />
			{
				ChildNodes = new List<DocumentNode>();
			}
		}
		
		/// <summary>
		/// Get/ set attributes on this node.
		/// </summary>
		public string this[string attrib]{
			get{
				Attributes.TryGetValue(attrib, out string value);
				return value;
			}
			set{
				Attributes[attrib] = value;
			}
		}

		/// <summary>
		/// Chainable append child.
		/// </summary>
		/// <param name="child"></param>
		/// <returns></returns>
		public DocumentNode AppendChild(DocumentNode child)
		{
			if (ChildNodes == null)
			{
				// This node does not support children
				return this;
			}
			ChildNodes.Add(child);
			return this;
		}

		/// <summary>
		/// Sets an attribute of the given name to an optional value in a chainable way.
		/// </summary>
		public DocumentNode With(string attrib, string value = null){
			Attributes[attrib] = value;
			return this;
		}
		
		/// <summary>
		/// stringifys the child nodes into the given builder.
		/// </summary>
		protected void FlattenChildren(List<DocumentNode> results, StringBuilder builder)
		{
			if(ChildNodes == null)
			{
				return;
			}
			
			for(var i=0;i<ChildNodes.Count;i++)
			{
				var node = ChildNodes[i];
				node.Flatten(results, builder);
			}
		}

		/// <summary>
		/// Flattens the hierarchy into a list of TextNode and SubstiteNode. Any 'static' html becomes part of TextNode.
		/// </summary>
		/// <returns></returns>
		public virtual void Flatten(List<DocumentNode> results, StringBuilder builder)
		{
			// Just add this node to the current text by default:
			builder.Append('<');
			builder.Append(NodeName);

			if (Attributes != null)
			{
				foreach (var kvp in Attributes)
				{
					builder.Append(' ');
					builder.Append(kvp.Key);

					if (kvp.Value != null)
					{
						builder.Append("=\"");
						builder.Append(HttpUtility.HtmlAttributeEncode(kvp.Value));
						builder.Append('\"');
					}
				}
			}

			if (ChildNodes == null && !SupressSelfClose)
			{
				builder.Append('/');
			}

			builder.Append('>');

			if (ChildNodes != null)
			{
				FlattenChildren(results, builder);
				builder.Append("</");
				builder.Append(NodeName);
				builder.Append('>');
			}
		}
		
		/// <summary>
		/// Flattens the hierarchy into a list of TextNode and SubstiteNode. Any 'static' html becomes part of TextNode.
		/// </summary>
		/// <returns></returns>
		public List<DocumentNode> Flatten()
		{
			var results = new List<DocumentNode>();
			var sb = new StringBuilder();
			Flatten(results, sb);

			if (sb.Length != 0)
			{
				// Add text node:
				results.Add(new TextNode(sb.ToString()));
			}

			return results;
		}

	}
	
	/// <summary>
	///	A HTML substitute node. These are special nodes which are evaluated on a per page load basis, unlike the rest of the document which is cached.
	/// </summary>
	public class SubstituteNode : DocumentNode
	{
		/// <summary>
		/// Create a new substitution node
		/// </summary>
		public SubstituteNode(Func<Context, Task<string>> onGenerate) : base("substitution"){
			OnGenerate = onGenerate;
		}
		
		/// <summary>
		/// The action to run during page loads.
		/// </summary>
		public Func<Context, Task<string>> OnGenerate;

		/// <summary>
		/// Flattens the DOM into a list of TextNode for any static html, and SubstituteNode's for any that changes per request.
		/// </summary>
		public override void Flatten(List<DocumentNode> results, StringBuilder currentText)
		{
			// Close previous text node:
			if (currentText.Length != 0)
			{
				// Add text node:
				results.Add(new TextNode(currentText.ToString()));
			}

			// Push this as-is:
			results.Add(this);

			// Start text again:
			currentText.Clear();
		}

	}
	
	/// <summary>
	///	A HTML text node.
	/// </summary>
	public class TextNode : DocumentNode
	{
		/// <summary>
		/// Create a new text node
		/// </summary>
		/// <param name="text"></param>
		public TextNode(string text) : base("text"){
			TextContent = text;
		}
		
		/// <summary>
		/// The text in this text node.
		/// </summary>
		public string TextContent;

		/// <summary>
		/// Convert to suitable html
		/// </summary>
		/// <param name="results"></param>
		/// <param name="builder"></param>
		public override void Flatten(List<DocumentNode> results, StringBuilder builder)
		{
			builder.Append(TextContent);
		}
		
	}

	/// <summary>
	/// A TextNode that has had its text encoded as utf8 bytes.
	/// </summary>
	public class RawBytesNode : DocumentNode
	{
		/// <summary>
		/// The raw bytes
		/// </summary>
		public byte[] Bytes;

		/// <summary>
		/// Create a new bytes node
		/// </summary>
		/// <param name="bytes"></param>
		public RawBytesNode(byte[] bytes) : base("bytes")
		{
			Bytes = bytes;
		}

	}

	/// <summary>
	/// A html document used when pre-rendering a page.
	/// </summary>
	public partial class Document : DocumentNode
	{
		/// <summary>
		/// Create a blank html doc
		/// </summary>
		public Document()
		{
			NodeName = "document";

			var doctype = new DocumentNode("!doctype", true).With("html");
			doctype.SupressSelfClose = true;
			AppendChild(doctype);
			
			var html = new DocumentNode("html");
			html.AppendChild(new DocumentNode("head"));
			html.AppendChild(new DocumentNode("body"));
			AppendChild(html);
		}
		
		/// <summary>
		/// The doctype node.
		/// </summary>
		public DocumentNode Doctype
		{
			get{
				return ChildNodes[0];
			}
		}
		
		/// <summary>
		/// The root html node.
		/// </summary>
		public DocumentNode Html
		{
			get
			{
				return ChildNodes[1];
			}
		}
		
		/// <summary>
		/// The head node.
		/// </summary>
		public DocumentNode Head
		{
			get
			{
				return Html.ChildNodes[0];
			}
		}
		
		/// <summary>
		/// The body node.
		/// </summary>
		public DocumentNode Body
		{
			get
			{
				return Html.ChildNodes[1];
			}
		}

		/// <summary>
		/// Convert to suitable html
		/// </summary>
		/// <param name="results"></param>
		/// <param name="builder"></param>
		public override void Flatten(List<DocumentNode> results, StringBuilder builder)
		{
			// Documents only render the children:
			FlattenChildren(results, builder);
		}
		
	}
}