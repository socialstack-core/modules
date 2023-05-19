using System;
using Api.Database;
using Api.Translate;
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
		public string From;
		
		/// <summary>
		/// Target URL to redirect to. SHOULD be site relative and start with a forwardslash always.
		/// Only use full URLs with a host name if they are not "this" site to make the redirect rules portable between environments.
		/// </summary>
		public string To;
	}

}