namespace Api.Startup
{
	/// <summary>
	/// For use when a piece of content has a list of a "child" piece of content.
	/// For example, a blog displays a list of blogPosts. BlogPost is a "child" admin page.
	/// </summary>
    public class ChildAdminPageOptions
    {
		/// <summary>
		/// Type name of the child type. It MUST have a field called {ParentTypeName}Id.
		/// </summary>
		public string ChildType;
		
		/// <summary>
		/// Fields to use in the nested list.
		/// </summary>
		public string[] Fields;
		
		/// <summary>
		/// Fields to be searchable. Leave null if you don't want to show search.
		/// </summary>
		public string[] SearchFields;
		
		/// <summary>
		/// False to disable the create button.
		/// </summary>
		public bool CreateButton = true;
	}
	
}