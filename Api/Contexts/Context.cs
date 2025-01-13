using System;
using System.Threading.Tasks;
using Api.Startup;

namespace Api.Contexts
{
	/// <summary>
	/// A context constructed primarily from a cookie value. 
	/// Uses other locale hints such as Accept-Lang when the user doesn't specifically have one set in the cookie.
	/// </summary>
	public partial class Context
	{
		/// <summary>
		/// Main cookie name (when impersonating)
		/// </summary>
		public static readonly string ImpersonationCookieName = "real_user";

		/// <summary>
		/// Create a default anonymous context.
		/// </summary>
		public Context() { }

		/// <summary>
		/// A date in the past used to set expiry on cookies.
		/// </summary>
		// private readonly static DateTimeOffset ThePast = new DateTimeOffset(1993, 1, 1, 0, 0, 0, TimeSpan.Zero);

		private static ContextService _contextService;

		/// <summary>
		/// Don't set this! Use the Options argument on e.g. aService.List calls - it will manage this field for you. True if this context will skip permissions checking.
		/// </summary>
		public bool IgnorePermissions;

		/// <summary>
		/// Set to false if editedUtc values shouldn't be updated.
		/// </summary>
		public bool PermitEditedUtcChange = true;
		
		/// <summary>
		/// Generates a new token for this ctx. Typically put into a cookie.
		/// </summary>
		/// <returns></returns>
		public string CreateToken()
		{
			if (_contextService == null)
			{
				_contextService = Services.Get<ContextService>();
			}

			return _contextService.CreateToken(this);
		}
	}

}
