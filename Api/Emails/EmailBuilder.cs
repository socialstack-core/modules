using Api.CanvasRenderer;
using Api.Translate;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Api.Emails;


/// <summary>
/// Used to auto install emails in a modular way, whilst also optionally adding templates.
/// </summary>
public class EmailBuilder
{
	/// <summary>
	/// Mandatory email key.
	/// </summary>
	public string Key;

	/// <summary>
	/// The primary content type if there is one.
	/// </summary>
	public string PrimaryContentType;

	/// <summary>
	/// Any includes on the primary type if any.
	/// </summary>
	public string PrimaryContentIncludes;

	/// <summary>
	/// Default email subject.
	/// </summary>
	public string Subject;

	/// <summary>
	/// Internal email name.
	/// </summary>
	public string Name;

	/// <summary>
	/// The generator for the email body.
	/// </summary>
	public Func<EmailBuilder, CanvasNode> BuildBody;

	private CanvasNode _body;

	/// <summary>
	/// The body 
	/// </summary>
	public CanvasNode Body {
		get {
			return _body;
		}
	}
	
	private EmailTemplate _template;

	/// <summary>
	/// The page 
	/// </summary>
	public EmailTemplate EmailTemplate {
		get {
			return _template;
		}
	}

	/// <summary>
	/// Adds the default template around the given body content.
	/// </summary>
	/// <param name="bodyContent"></param>
	public CanvasNode AddTemplate(params CanvasNode[] bodyContent)
	{
		var templateNode = new CanvasNode("Admin/Template");
		templateNode.With("templateKey", "email_default");

		var bodyRoot = new CanvasNode();

		if (bodyContent != null)
		{
			bodyRoot.Content = [.. bodyContent];
		}

		templateNode.Roots = new Dictionary<string, CanvasNode>
		{
			{ "body", bodyRoot }
		};
		return templateNode;
	}

	/// <summary>
	/// The node to add additional generic content to. If AddTemplate was used, then the content root 
	/// is actually the main body area of the template. Otherwise, it is the root itself.
	/// </summary>
	/// <returns></returns>
	public CanvasNode GetContentRoot()
	{
		if (_body == null)
		{
			return null;
		}

		if (_body.Module == "Admin/Template")
		{
			// It's got a template wrapper.
			var roots = _body.Roots;

			if (roots == null)
			{
				return _body;
			}

			if (roots.TryGetValue("body", out CanvasNode result))
			{
				return result;
			}

			// The first root otherwise.
			var first = roots.FirstOrDefault();

			if (first.Value != null)
			{
				return first.Value;
			}
		}

		return _body;
	}

	/// <summary>
	/// Builds the initial body and template objects.
	/// </summary>
	public virtual void Build()
	{
		_body = BuildBody(this);

		var template = new EmailTemplate()
		{
			Name = Name,
			Key = Key,
			PrimaryContentType = PrimaryContentType,
			PrimaryContentIncludes = PrimaryContentIncludes,
			Subject = new Localized<string>(Subject)
		};

		_template = template;
	}
}