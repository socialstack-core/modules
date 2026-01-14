using Api.CanvasRenderer;
using Api.Database;
using Api.Startup;
using Api.Translate;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Api.Pages;


/// <summary>
/// Used to auto install pages in a modular way, whilst also optionally adding templates.
/// </summary>
public partial class PageBuilder
{
	/// <summary>
	/// Mandatory page key. Start it with admin_ for any admin page.
	/// </summary>
	public string Key;
	
	/// <summary>
	/// Optional URL. Will be added as a permalink pointing at 
	/// either this page or the primary type if the key is a primary one.
	/// </summary>
	public string Url;

	/// <summary>
	/// Default page title.
	/// </summary>
	public string Title;
	
	/// <summary>
	/// The parent nav menu item.
	/// </summary>
	public uint NavMenuParentId;

	/// <summary>
	/// Setting this to true is a shortcut for representing an exclusion for the 'public' role from the page.
	/// </summary>
	public bool LoginRequired {
		set {
			if (value)
			{
				// public role is always #6. Roles.Public.Id isn't actually reliable when this is used 
				// because we might be, for example, installing pages inside the role service constructor.
				CustomMappings.Set("RoleExclusions", [6]);
			}
		}
	}

	/// <summary>
	/// Custom mappings for the page if any.
	/// </summary>
	public MappingData CustomMappings;

	private string _primaryContentIncludes;

	/// <summary>
	/// Primary content includes, if any.
	/// </summary>
	public string PrimaryContentIncludes
	{
		get {
			return _primaryContentIncludes;
		}
		set {
			_primaryContentIncludes = value;

			if (_page != null)
			{
				_page.PrimaryContentIncludes = value;
			}
		}
	}

	/// <summary>
	/// True if the primary content is loaded via a revision.
	/// </summary>
	public bool PrimaryContentIsRevisions;

	/// <summary>
	/// The primary content type if there is one. Derived from the key if not specified.
	/// </summary>
	public string PrimaryContentType;

	private string _adminNavMenuTitle;

	/// <summary>
	/// A title for the admin nav menu, if any.
	/// If you set an icon but no AdminNavMenuTitle, this will be equal to Title.
	/// </summary>
	public string AdminNavMenuTitle {
		get {
			if (_adminNavMenuTitle == null)
			{
				if (!string.IsNullOrEmpty(AdminNavMenuIcon))
				{
					return Title;
				}
			}

			return _adminNavMenuTitle;
		}
		set {
			_adminNavMenuTitle = value;
		}
	}

	/// <summary>
	/// An icon for the admin nav menu, if any. 
	/// Specifying only an icon but no title will act like AdminNavMenuTitle was equal to Title.
	/// </summary>
	public string AdminNavMenuIcon;

	/// <summary>
	/// If this is an admin panel page, this is a convenience reference to the set of options given.
	/// </summary>
	public AdminPageOptions AdminPageOptions;

	/// <summary>
	/// The generator for the page body.
	/// </summary>
	public Func<PageBuilder, CanvasNode> BuildBody;

	private CanvasNode _body;

	/// <summary>
	/// The body 
	/// </summary>
	public CanvasNode Body {
		get {
			return _body;
		}
	}
	
	private Page _page;

	/// <summary>
	/// The page 
	/// </summary>
	public Page Page {
		get {
			return _page;
		}
	}

	/// <summary>
	/// If this page is associated with a main content type (usually admin panel pages which list or edit this type - see also IsAdmin and PageType)
	/// then this is a convenience reference to the type itself. Optional.
	/// </summary>
	public Type ContentType;

	/// <summary>
	/// Can be null. Gets set during installation of bulkier common page types: primarily that's the admin panel.
	/// </summary>
	public CommonPageType PageType;

	/// <summary>
	/// True if this is an admin page.
	/// </summary>
	public bool IsAdmin => Key != null && (Key == "admin" || Key.StartsWith("admin_"));

	/// <summary>
	/// Adds the default site or admin template around the given body content.
	/// </summary>
	/// <param name="bodyContent"></param>
	public CanvasNode AddTemplate(params CanvasNode[] bodyContent)
	{
		var templateNode = new CanvasNode("Admin/Template");
		templateNode.With("templateKey", IsAdmin ? "admin_default" : "site_default");

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
	/// Convenience mechanism for adding a new admin tab if the page being installed is an AutoForm based one.
	/// Does nothing if the tab already exists by key.
	/// </summary>
	/// <param name="tab"></param>
	public void AddAdminTab(AdminTab tab)
	{
		if (AdminPageOptions == null)
		{
			Log.Error("PageService", "You're trying to add an admin tab '" + tab.Name + "' to a page that isn't on the admin panel. The page is  '" + Key + "'. Are you missing a logic check?");
			return;
		}

		AdminPageOptions.AddTab(tab);

		// Update the tab set:
		var autoFormNode = GetContentRoot().Find("Admin/AutoForm");

		if (autoFormNode != null)
		{
			autoFormNode.With("tabs", AdminPageOptions.Tabs);
		}
	}

	/// <summary>
	/// Overrides the applied template with the named one if a template is present (doing nothing and returning false otherwise).
	/// </summary>
	/// <param name="templateName"></param>
	public bool SetTemplate(string templateName)
	{
		if (_body == null || _body.Module != "Admin/Template")
		{
			return false;
		}

		_body.With("templateKey", templateName);
		return true;
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
	/// Builds the initial body and page objects.
	/// </summary>
	public virtual void Build()
	{
		_body = BuildBody(this);

		var page = new Page(){
			Url = Url,
			Key = Key,
			Mappings = CustomMappings,
			Title = new Localized<string>(Title),
			PrimaryContentIncludes = PrimaryContentIncludes,
			PrimaryContentType = PrimaryContentType,
			PrimaryContentIsRevisions = PrimaryContentIsRevisions
		};

		_page = page;
	}

	/// <summary>
	/// Convenience mechanism for setting admin relative URLs.
	/// </summary>
	public string AdminRelativeUrl
	{
		set
		{
			if (!value.StartsWith("/"))
			{
				value = "/" + value;
			}

			Key = "admin" + (value == "/" ? "" : value.ToLower().Replace('/', '_'));
			Url = "/en-admin" + value;
		}
	}

}