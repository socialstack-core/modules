using Api.CanvasRenderer;
using Api.Translate;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Api.Startup
{
	/// <summary>
	/// Holds additional configuration options when creating a group of admin panel pages.
	/// </summary>
	public class AdminPageOptions
	{
		/// <summary>
		/// The includes on the edit page.
		/// </summary>
		public string EditIncludes;

		/// <summary>
		/// The includes on the list page.
		/// </summary>
		public string ListIncludes;

		/// <summary>
		/// The navigation menu label.
		/// </summary>
		public Localized<string> NavMenuLabel;

		/// <summary>
		/// The navigation menu icon.
		/// </summary>
		public string NavMenuIcon;

		/// <summary>
		/// What is the parent?
		/// </summary>
		public string NavMenuParentKey;

		/// <summary>
		/// The fields which are used on the list page.
		/// </summary>
		public string[] ListFields
		{
			set
			{
				List<AutoListColumn> columns = null;

				if (value != null)
				{
					columns = [];
					foreach (var item in value)
					{
						columns.Add(new AutoListColumn
						{
							Field = item
						});
					}
				}

				ListColumns = columns;
			}
		}

		/// <summary>
		/// Overwrite the field names in list field, the key is the list field name, the value
		/// is the desired output, this also allows a you to specify a sort field. 
		/// </summary>
		public List<AutoListColumn> ListColumns;

		/// <summary>
		/// Used to define tabs on the admin edit/ create page. Use Data("tab", "key_here") to tell a particular field 
		/// which tab to appear in. If a field is not specified but these tabs are configured, the field defaults to the 1st tab in the set.
		/// </summary>
		public List<AdminTab> Tabs;

		/// <summary>
		/// A shortcut for specifying that your type has some kind of sub-type.
		/// For example, the NavMenu admin page specifies a child type of NavMenuItem, meaning each NavMenu ends up with a list of NavMenuItems.
		/// Make sure you specify the fields that'll be visible from the child type in the list on the parent type.
		/// For example, if you'd like each child entry to show its Id and Title fields, specify new string[]{"id", "title"}.
		/// </summary>
		public AdminPageOptions ChildType;

		/// <summary>
		/// The service requesting the admin page installation. Don't set this unless you intentionally want to mislead it.
		/// </summary>
		public AutoService ContentService;

		/// <summary>
		/// A token supporting string to use for the edit page title, e.g. "${content.customName}" to display the specified field.
		/// </summary>
		public string EditTitleToken;


		/// <summary>
		/// Adds the given tab. 
		/// Does nothing if the tab already exists by key.
		/// </summary>
		/// <param name="tab"></param>
		/// <returns>The tab that was actually added (or the existing one)</returns>
		public AdminTab AddTab(AdminTab tab)
		{
			if (Tabs == null)
			{
				Tabs = new List<AdminTab>() {
					new AdminTab("Details", "details")
				};
			}

			var existing = Tabs.Find(existingTab => existingTab.Key == tab.Key);

			if (existing != null)
			{
				return existing;
			}

			Tabs.Add(tab);

			return tab;
		}
	}

	/// <summary>
	/// Specifies a custom column in
	/// AutoList.
	/// </summary>
	public class AutoListColumn
	{
		/// <summary>
		/// The header display name
		/// </summary>
		public string Label;

		/// <summary>
		/// What field does it need to sort on?
		/// </summary>
		public string Field;

		/// <summary>
		/// Optional module to output a certain module.
		/// </summary>
		public string Module;

		/// <summary>
		/// Indicate if the field is safe to search
		/// </summary>
		public bool IsSearchable;

		/// <summary>
		/// True if this is the title field (used for default sorting).
		/// </summary>
		public bool Title;
	}

	/// <summary>
	/// Used to define tabs on the admin edit/ create page.
	/// </summary>
	public class AdminTab
	{
		/// <summary>
		/// The textual name which appears on the tab.
		/// </summary>
		public string Name;

		/// <summary>
		/// A lowercase ascii with underscores key such as "general_details". Use this in Data("tab", "key_here") to 
		/// declare which tab a given field appears in. If a field iss
		/// </summary>
		public string Key;

		/// <summary>
		/// A json string for optional predefined tab canvas content.
		/// </summary>
		public string ContentJson
		{
			get
			{
				if (Content == null)
				{
					return null;
				}

				return Content.ToJson();
			}
		}

		/// <summary>
		/// Optional predefined canvas content for this tab node.
		/// </summary>
		[JsonIgnore]
		public CanvasNode Content;

		/// <summary>
		/// Creates a new admin tab with the given english name and key.
		/// </summary>
		/// <param name="nameEn"></param>
		/// <param name="key"></param>
		public AdminTab(string nameEn, string key)
		{
			Name = nameEn;
			Key = key;
		}
	}

}