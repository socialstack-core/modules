using Api.Contexts;
using Api.SocketServerLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
		/// Parent doc node.
		/// </summary>
		public DocumentNode Parent;

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
		/// Inserts the given node before the given one, which should be a child of this. 
		/// Use e.g. beforeThis.Parent.InsertBefore(thing, beforeThis); if you don't know what the parent is.
		/// </summary>
		/// <param name="addThis"></param>
		/// <param name="beforeThis"></param>
		/// <returns></returns>
		public DocumentNode InsertBefore(DocumentNode addThis, DocumentNode beforeThis)
		{
			if (beforeThis.Parent != this)
			{
				// Before this isn't a child of this, so add to end:
				AppendChild(addThis);
				return this;
			}

			if (ChildNodes == null)
			{
				// Node doesn't support children
				return this;
			}

			ChildNodes.Insert(ChildNodes.IndexOf(beforeThis), addThis);
			return this;
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
			child.Parent = this;
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
		public SubstituteNode(Func<Context, Writer, PageWithTokens, ValueTask> onGenerate) : base("substitution"){
			OnGenerate = onGenerate;
		}
		
		/// <summary>
		/// The action to run during page loads.
		/// </summary>
		public Func<Context, Writer, PageWithTokens, ValueTask> OnGenerate;

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

		/// <summary>
		/// Convert to suitable html
		/// </summary>
		/// <param name="results"></param>
		/// <param name="currentText"></param>
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
	/// A document used when pre-rendering a page.
	/// </summary>
	public partial class Document : DocumentNode
	{
		/// <summary>
		/// The generated doc title. This originated from the source page's title field.
		/// </summary>
		public string Title;
		/// <summary>
		/// The Page that this doc originated from.
		/// </summary>
		public Page SourcePage;
		/// <summary>
		/// The contentTypeId of the primary object, if there is one. 0 otherwise.
		/// </summary>
		public int PrimaryContentTypeId;
		/// <summary>
		/// The primary object, if there is one.
		/// </summary>
		public object PrimaryObject;
		/// <summary>
		/// The type of the primary object, if there is one. Same as PrimaryObject.GetType()
		/// </summary>
		public Type PrimaryObjectType;
		/// <summary>
		/// The AutoService that provided the primary object, if there is one.
		/// </summary>
		public AutoService PrimaryObjectService;
		/// <summary>
		/// site relative URL of this doc.
		/// </summary>
		public string Path;

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
		/// The pageService.
		/// </summary>
		private static PageService _pageService;

		/// <summary>
		/// Gets a named meta field from the primary object. You can specify a meta field with [meta("fieldName")] in your entity.
		/// Note that [meta("title")] and [meta("description")] are 'guessed' automatically if you haven't explicitly declared them in your entity.
		/// If the meta field is not set on the primary object, this function will then attempt to read the meta field from the Page object instead.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="fieldName"></param>
		/// <returns></returns>
		public async ValueTask<object> GetMeta(Context context, string fieldName)
		{
			if (PrimaryObjectService != null && PrimaryObject != null)
			{
				var meta = await PrimaryObjectService.GetMetaFieldValue(context, fieldName, PrimaryObject);

				if (meta != null)
				{
					return meta;
				}
			}

			// Otherwise, try the page instead:
			if (_pageService == null)
			{
				_pageService = Api.Startup.Services.Get<PageService>();
			}

			return await _pageService.GetMetaFieldValue(context, fieldName, SourcePage);
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
		/// A reference to the main js script element. Useful for inserting before it.
		/// </summary>
		public DocumentNode MainJs;

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