using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Api.Configuration;
using Api.Database;
using Api.Eventing;
using Api.Signatures;


namespace Api.Contexts
{
	/// <summary>
	/// Used to establish primary user context - role, locale and the user ID - when possible.
	/// This is signature based - it doesn't generate any database traffic.
	/// </summary>
	public class ContextService
    {
		/// <summary>
		/// Tracks any active revocations. A revoke occurs when either a user is forcefully logged out (e.g. account was declared stolen)
		/// Or because their role was explicitly changed. Essentially rare admin tasks. 
		/// A role change can be automatically reissued but a ref revoke requires logging in again.
		/// </summary>
		// TODO: Populate the revoke map on load (#208).
		private Dictionary<int, int> RevocationMap = new Dictionary<int, int>();

		/// <summary>
		/// Maps lowercase field names to the info about them.
		/// </summary>
		private Dictionary<string, ContextFieldInfo> Fields = new Dictionary<string, ContextFieldInfo>();
		private List<ContextFieldInfo> FieldList = new List<ContextFieldInfo>();
		
		/// <summary>
		/// Maps a content type ID to the context field info. Your context property must end with 'Id' to get an entry here.
		/// </summary>
		private Dictionary<int, ContextFieldInfo> ContentTypeToFieldInfo = new Dictionary<int, ContextFieldInfo>();
		private SignatureService _signatures;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
        public ContextService(SignatureService signatures)
        {
			_signatures = signatures;

			// Load all the props now.
			var fields = typeof(Context).GetProperties(BindingFlags.Public | BindingFlags.Instance);

			var defaultValueChecker = new Context();

			foreach (var field in fields)
			{
				// must be an int field.
				if (field.PropertyType != typeof(int))
				{
					continue;
				}
				
				var lcName = field.Name.ToLower();
				var getMethod = field.GetGetMethod();
				
				var defaultValue = (int)getMethod.Invoke(defaultValueChecker, System.Array.Empty<object>());
				
				var fld = new ContextFieldInfo()
				{
					Set = field.GetSetMethod(),
					Get = getMethod,
					Property = field,
					Name = field.Name,
					LowercaseNameWithDash = lcName + '-',
					DefaultValue = defaultValue
				};
				
				if(field.Name.EndsWith("Id")){
					// E.g. UserId, LocaleId.
					// Get content type ID:
					var contentTypeId = ContentTypes.GetId(field.Name.Substring(0, field.Name.Length - 2));
					ContentTypeToFieldInfo[contentTypeId] = fld;
				}
				
				Fields[lcName] = fld;
				FieldList.Add(fld);
			}
        }
		
		/// <summary>
		/// Gets Context field info for the given contentType. Null if it doesn't exist in Context.
		/// </summary>
		public ContextFieldInfo FieldByContentType(int contentTypeId)
		{
			ContentTypeToFieldInfo.TryGetValue(contentTypeId, out ContextFieldInfo result);
			return result;
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
		/// Cookie domain
		/// </summary>
		private static string _domain = null;
		
		/// <summary>
		/// Cookie domain to use
		/// </summary>
		/// <returns></returns>
		public string GetDomain()
		{
			if (_domain == null)
			{
				if(AppSettings.Configuration["CookieDomain"] != null)
				{
					_domain = AppSettings.Configuration["CookieDomain"];
				}
				else
				{
					#if DEBUG
						// Localhost
						return null;
					#else
					_domain = AppSettings.Configuration["PublicUrl"].Replace("https://", "");
					if (_domain.StartsWith("www."))
					{
						_domain = _domain.Substring(4);
					}
					
					var fwdSlash = _domain.IndexOf('/');
					
					if(fwdSlash != -1)
					{
						// Trim everything else off:
						_domain = _domain.Substring(0, fwdSlash);
					}
					
					#endif
				}
			}

			return _domain;
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
				return new Context()
				{
					CookieState = 6
				};
            }

			// Default token str format is:
			// userid-X-roleid-Y-userref-Z|Base64SignatureOfEverythingElse

			var tokenSig = tokenStr.Split('|');

            if (tokenSig.Length != 2)
            {
				return new Context()
				{
					CookieState = 5
				};
            }

			// Verify the signature first:
			if (!_signatures.ValidateSignature(tokenSig[0], tokenSig[1]))
			{
				return new Context() {
					CookieState = 4
				};
			}
			
			// Verified! Build the token based on what was in the cookie:
			var tokenParts = tokenSig[0].Split('-');

			var length = tokenParts.Length;

			if ((length % 2) != 0)
			{
				// Must have an even # of parts.
				return new Context()
				{
					CookieState = 7
				};
			}
			
			var context = new Context();

			// Every even token part is a field name, odd parts are the value.
			// Default values - the value seen when creating a new context - are not stored.
			var args = new object[1];

			for (var i = 0; i < length; i += 2)
			{
				var fieldName = tokenParts[i];
				var fieldValue = tokenParts[i + 1];

				if (!Fields.TryGetValue(fieldName, out ContextFieldInfo field))
				{
					// Removed field. We can ignore this and permit all other changes.
					continue;
				}

				if (!int.TryParse(fieldValue, out int id))
				{
					// Ancient cookie.
					continue;
				}

				args[0] = id;

				field.Set.Invoke(context, args);
			}
			
			// If we don't have a revocation for the given user ref then continue.
			// This means a significant amount of API requests go through without a single auth related database hit.

			if (RevocationMap.TryGetValue(context.UserId, out int revokedRefs))
			{
				if (context.UserRef <= revokedRefs)
				{
					// This token is revoked permanently.
					return null;
				}
			}
			

			return context;
		}

		private object[] _emptyArgs = System.Array.Empty<object>();

		/// <summary>
		/// Creates a signed token for the given context.
		/// </summary>
		/// <returns></returns>
		public string CreateToken(Context context)
		{
			var builder = new StringBuilder();

			bool first = true;

			foreach (var field in FieldList)
			{
				var value = (int)field.Get.Invoke(context, _emptyArgs);

				if (value == field.DefaultValue)
				{
					continue;
				}

				if (first)
				{
					first = false;
				}
				else
				{
					builder.Append('-');
				}

				builder.Append(field.LowercaseNameWithDash);
				builder.Append(value);
			}

			var tokenStr = builder.ToString();
			tokenStr += "|" + _signatures.Sign(tokenStr);
            return tokenStr;
        }
        
    }
}
