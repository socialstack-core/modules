using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Signatures;


namespace Api.Contexts
{
	/// <summary>
	/// Used to establish primary user context - role, locale and the user ID - when possible.
	/// This is signature based - it doesn't generate any database traffic.
	/// </summary>
	public class ContextService : IContextService
    {
		/// <summary>
		/// Tracks any active revocations. A revoke occurs when either a user is forcefully logged out (e.g. account was declared stolen)
		/// Or because their role was explicitly changed. Essentially rare admin tasks. 
		/// A role change can be automatically reissued but a ref revoke requires logging in again.
		/// </summary>
		#warning todo - populate the revoke map on load
		private Dictionary<int, int> RevocationMap = new Dictionary<int, int>();

		private ISignatureService _signatures;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
        public ContextService(ISignatureService signatures)
        {
			_signatures = signatures;
        }

		/// <summary>
		/// The name of the cookie in use.
		/// </summary>
        public string CookieName
        {
            get
            {
                return "user";
            }
        }

		/// <summary>
		/// Revokes all previous login tokens for the given user.
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="loginRevokeCount"></param>
		public void Revoke(int userId, int loginRevokeCount)
		{
			RevocationMap[userId] = loginRevokeCount;
		}

		/// <summary>
		/// Gets a login token from the given cookie text.
		/// </summary>
		/// <param name="tokenStr"></param>
		/// <returns></returns>
		public Context Get(string tokenStr)
        {
            if (tokenStr == null)
            {
                return null;
            }

			// Token str format is:
			// UserID-RoleID-RoleRefVersion|Base64SignatureOfEverythingElse

			var tokenSig = tokenStr.Split('|');

            if (tokenSig.Length != 2)
            {
                return null;
            }

			// Verify the signature first:
			if (!_signatures.ValidateSignature(tokenSig[0], tokenSig[1]))
			{
				return null;
			}
			
			// Verified! Build the token based on what was in the cookie:
			var tokenParts = tokenSig[0].Split('-');

			if (tokenParts.Length != 3)
			{
				return null;
			}

			// Must all be integers:
			if (!int.TryParse(tokenParts[0], out int userId))
			{
				return null;
			}

			if (!int.TryParse(tokenParts[1], out int userRef))
			{
				return null;
			}

			if (!int.TryParse(tokenParts[2], out int role))
			{
				return null;
			}

			// If we don't have a revocation for the given user ref then continue.
			// This means a significant amount of API requests go through without a single auth related database hit.

			if (RevocationMap.TryGetValue(userId, out int revokedRefs))
			{
				if (userRef <= revokedRefs)
				{
					// This token is revoked permanently.
					return null;
				}
			}

			var token = new Context()
			{
				UserId = userId,
				UserRef = userRef,
				RoleId = role
			};
			
			return token;
		}

		/// <summary>
		/// Creates a signed token for the given user.
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="userRef">The login ref for this user.</param>
		/// <param name="roleId">The user's role ID.</param>
		/// <returns></returns>
		public string CreateToken(int userId, int userRef, int roleId)
		{
			// Cookie format is:
			// UserID-UserRef-RoleID|Base64SignatureOfEverythingElse

			var tokenStr = userId + "-" + userRef + "-" + roleId;
			tokenStr += "|" + _signatures.Sign(tokenStr);
            return tokenStr;
        }
        
    }
}
