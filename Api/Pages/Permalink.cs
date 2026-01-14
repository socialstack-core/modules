using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Pages
{

	/// <summary>
	/// A Permalink. The most recent permalink for a given target URL is canonical.
	/// If there is no such source URL, the target is canonical.
	/// Unlike a redirect, a permalink is primarily a URL rewrite. Their purpose is to allow very complex URLs
	/// for e.g. products with deeply nested categories, whilst not breaking the URL when any of the products change.
	/// Instead older permalinks just become redirects to the latest, canonical one.
	/// Has an index which blocks creation of duplicates at the cluster level.
	/// </summary>
	[DatabaseIndex(Fields = new string[]{ "Url", "Target" }, Direction = "ASC", Unique = true)]
	public partial class Permalink : UserCreatedContent<uint>
	{
		/// <summary>
		/// The source URL. Always an absolute path ("/hello-world") which can contain ${tokens}. These token values appear 
		/// in the JS via useRouter, and in the C# via the PageWithTokens struct.
		/// </summary>
		public string Url;

		/// <summary>
		/// The target. Can be an absolute path ("/hello-world") but is almost always a constant 'target locator'.
		/// Currently supported target locators are:
		/// - A specific page "page:42"
		/// - Primary page target locator (see the PermalinkService.CreatePrimaryTargetLocator method). "primary:user:42", "primary:user", "admin_primary:user", "admin_primary:user:42"
		/// 
		/// If your locator does specify a primary page but does not specify the content ID (i.e. it's pointing at general use primary page) then your Url must contain an ${type.id} token.
		/// For example, /users/${user.id} targeting "primary:user"
		/// /users/${user.id} targeting "page:42" which in turn has a key of "primary:user"
		/// Essentially when the ID is not known, it will be resolved from an ID token. If you want a fancier URL with a slug etc, then you must generate that as a dedicated permalink. 
		/// This is to achieve the main goal of permalinks: historical URLs are preserved when your potentially more dynamic (non-ID slugs) fields change.
		/// </summary>
		public string Target;
	}

	/// <summary>
	/// Just a URL and target for a permalink.
	/// </summary>
	public struct PermalinkUrlTarget
	{
		/// <summary>
		/// The source URL. See Permalink.Url for more details.
		/// </summary>
		public string Url;

		/// <summary>
		/// The target. See Permalink.Target for more details.
		/// </summary>
		public string Target;
	}

}