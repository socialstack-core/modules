namespace Api.Pages
{
	
	/// <summary>
	/// The "scope" of a particular URL. Each scope of URLs can have one canonical URL per content type.
	/// </summary>
	public partial class UrlGenerationScope
	{
		/// <summary>
		/// Main frontend of the site. This is everything that isn't /en-admin/.
		/// </summary>
		public static UrlGenerationScope UI = new UrlGenerationScope(null, 1, 10);
		
		/// <summary>
		/// The admin panel.
		/// </summary>
		public static UrlGenerationScope Admin = new UrlGenerationScope("/en-admin", 2, 20);
		
		/// <summary>
		/// All scopes, sorted by priority.
		/// </summary>
		public static UrlGenerationScope[] All = new UrlGenerationScope[]{UI, Admin};
		
		
		/// <summary>
		/// The prefix of URLs that will end up in this scope.
		/// </summary>
		public string Prefix;
		
		/// <summary>
		/// The ID of this scope. Used as an array index for an array of scopes.
		/// </summary>
		public int Id;
		
		/// <summary>
		/// The higher this number is, the later the scope is collected.
		/// We loop over scopes by order of their priority to collect URLs that the scope will handle.
		/// </summary>
		public int Priority;
		
		/// <summary>
		/// </summary>
		public UrlGenerationScope(string prefix, int id, int priority)
		{
			Priority = priority;
			Prefix = prefix;
			Id = id;
		}
		
	}
	
}