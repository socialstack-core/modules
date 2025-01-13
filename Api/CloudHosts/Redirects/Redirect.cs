using Api.AutoForms;
using Api.Users;


namespace Api.Redirects
{
	
	/// <summary>
	/// A Redirect
	/// </summary>
	public partial class Redirect : VersionedContent<uint>
	{
		/// <summary>
		/// Source URL to redirect from. MUST be site relative and start with a forwardslash always.
		/// For example, "/hello-world/"
		/// If it ends with a forward slash, then both with and without the fwdslash will be redirected.
		/// I.e. "/hello-world/" and "/hello-world"
		/// </summary>
		public string From;
		
		/// <summary>
		/// Target URL to redirect to. SHOULD be site relative and start with a forwardslash always.
		/// Only use full URLs with a host name if they are not "this" site to make the redirect rules portable between environments.
		/// </summary>
		public string To;

		/// <summary>
		/// True if this redirect should be considered permanent (i.e. a 301);
		/// Will default to a temporary 302 redirect if not set
		/// </summary>
		[Data("hint", "Redirect will be temporary (302) by default - select here for a permanent (301) redirect. PLEASE NOTE - do not use this option if you are likely to require this URL again")]
		public bool PermanentRedirect = false;
	}

}