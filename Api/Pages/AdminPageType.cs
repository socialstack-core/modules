namespace Api.Pages
{
	
	/// <summary>
	/// The types of admin page.
	/// </summary>
	public partial class AdminPageType
	{
		/// <summary>
		/// Admin pages of the form /en-admin/{CONTENT_TYPE_NAME}
		/// </summary>
		public static AdminPageType List = new AdminPageType("list");
		
		/// <summary>
		/// Admin pages of the form /en-admin/{CONTENT_TYPE_NAME}/{ID}
		/// </summary>
		public static AdminPageType Single = new AdminPageType("single");
		
		/// <summary>
		/// A key for this admin page type.
		/// </summary>
		public string Key;
		
		/// <summary>
		/// Create a new admin page type with the given key.
		/// </summary>
		public AdminPageType(string key)
		{
			Key = key;
		}
		
	}
	
}