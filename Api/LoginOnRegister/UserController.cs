using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.Contexts;
using Api.Startup;
using Api.Users;

namespace Api.Users
{
    /// <summary>
    /// Handles user account endpoints.
    /// </summary>
	public partial class UserController
    {
		/// <summary>
		/// POST /v1/user/registerandlogin/
		/// Registers a new account using the given details, then logs it straight in and returns the context.
		/// Halts if registration fails (e.g. the account already exists).
		/// </summary>
		[HttpPost("registerandlogin")]
		public async ValueTask<Context> RegisterAndLogin(Context context, [FromBody] JObject body)
		{
			// The standard create flow enforces password policy, unique usernames/emails and sets a default role.
			// It halts here if the account can't be created (e.g. it already exists):
			var user = await CreateInternal(_service, context, body);

			if (user == null)
			{
				throw new PublicException("Unable to register an account with those details.", "user_not_found");
			}

			// Log the account straight in by putting it on the context. Returning it also emits the login token cookie like login does:
			context.User = user;

			return context;
		}
    }
}