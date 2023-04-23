using System;
using Api.Permissions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Api.Signatures;
using Api.Startup;
using Api.Eventing;


namespace Api.Contexts
{
	public partial class Context
	{
		
		private static RoleService _roles;
		
		/// <summary>
		/// Create a context for no particular user but with the given role.
		/// </summary>
		/// <param name="role"></param>
		public Context(Role role) {
			_roleId = role.Id;
		}

		/// <summary>
		/// Underlying role ID.
		/// </summary>
		private uint _roleId = 6;  // Public is the default

		/// <summary>
		/// The role ID from the token.
		/// </summary>
		public uint RoleId {
			get
			{
				return _roleId;
			}
		}

		/// <summary>
		/// Full role object.
		/// </summary>
		private Role _role;

		/// <summary>
		/// Role. Null indicates a broken AuthUser instance or user of a Role ID which probably hasn't been setup.
		/// </summary>
		public Role Role
		{
			get
			{
				if (_roles == null)
				{
					_roles = Services.Get<RoleService>();
				}

				if (_role != null)
				{
					return _role;
				}

				// RoleService is always cached:
				var cache = _roles.GetCacheForLocale(1);
				_role = cache.Get(RoleId);
				return _role;
			}
		}

		/// <summary>
		/// Creates a remote token for this context. 
		/// Essentially, this allows this context to be used on a remote thirdparty system, provided 
		/// that the remote system has permitted the public key of the given keypair by adding it to its SignatureService Hosts config.
		/// </summary>
		/// <param name="hostName"></param>
		/// <param name="keyPair"></param>
		/// <returns></returns>
		public string CreateRemoteToken(string hostName, KeyPair keyPair)
		{
			if (_contextService == null)
			{
				_contextService = Services.Get<ContextService>();
			}

			return _contextService.CreateRemoteToken(this, hostName, keyPair);
		}
		
		/// <summary>
		/// Sends a token as both a header and a cookie for the given response.
		/// </summary>
		/// <param name="response"></param>
		public void SendToken(HttpResponse response)
		{
			var token = CreateToken();
			response.Headers.Append("Token", token);

			var expiry = DateTime.UtcNow.AddDays(120);

			if (_contextService == null)
			{
				_contextService = Services.Get<ContextService>();
			}

			response.Cookies.Append(
					_contextService.CookieName,
					token,
					new Microsoft.AspNetCore.Http.CookieOptions()
					{
						Path = "/",
						Expires = expiry,
						Domain = _contextService.GetDomain(LocaleId),
						IsEssential = true,
						HttpOnly = true,
#if DEBUG
						Secure = false,
#else
						Secure = true,
#endif
						SameSite = SameSiteMode.Lax
					}
				);
		}
	}
}