using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using Api.Configuration;
using Api.Database;
using Api.Signatures;
using Api.SocketServerLibrary;
using Api.Startup;
using Api.Users;


namespace Api.Contexts
{
	/// <summary>
	/// Used to establish primary user context - role, locale and the user ID - when possible.
	/// This is signature based - it doesn't generate any database traffic.
	/// </summary>
	public class ContextService : AutoService
    {
		/// <summary>
		/// "null"
		/// </summary>
		private static readonly byte[] NullText = new byte[] { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };
		
		/// <summary>
		/// "1"
		/// </summary>
		private static readonly byte[] VersionField = new byte[] { (byte)'1' };

		private readonly SignatureService _signatures;
		private readonly UserService _users;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
        public ContextService(SignatureService signatures, UserService users)
        {
			_signatures = signatures;
			_users = users;
        }

		/// <summary>
		/// Serialises the given context.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="targetStream"></param>
		/// <returns></returns>
		public async ValueTask ToJson(Context context, Stream targetStream)
		{
			var writer = Writer.GetPooled();
			writer.Start(null);

			await ToJson(context, writer);

			// Copy to output:
			await writer.CopyToAsync(targetStream);

			// Release writer when fully done:
			writer.Release();
		}

		/// <summary>
		/// Serialises the given context into the given writer.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="writer"></param>
		/// <returns></returns>
		public async ValueTask ToJson(Context context, Writer writer)
		{
			writer.Write((byte)'{');

			// Almost the same as virtual field includes, except they're always included.
			var first = true;

			for (var i = 0; i < ContextFields.FieldList.Count; i++)
			{
				var fld = ContextFields.FieldList[i];

				if (fld.ViewCapability != null && context.Role.GetGrantRule(fld.ViewCapability) == null)
				{
					continue;
				}

				if (first)
				{
					first = false;
				}
				else
				{
					writer.Write((byte)',');
				}

				// Write the header (Also includes a comma at the start if i!=0):
				writer.Write(fld.JsonFieldHeader, 0, fld.JsonFieldHeader.Length);

				// Note that this allocates due to the boxing of the id.
				var id = (uint)fld.PrivateFieldInfo.GetValue(context);

				if (id == 0)
				{
					// null. This exception is important for permissions, 
					// as a user may not be able to access this object type yet.
					writer.Write(NullText, 0, 4);
				}
				else
				{
					// Write the value:
					await fld.Service.OutputById(context, id, writer, DataOptions.IgnorePermissions, null);
				}
			}

			writer.Write((byte)'}');
		}

		/// <summary>
		/// Serialises the given context.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public async ValueTask<string> ToJsonString(Context context)
		{
			var writer = Writer.GetPooled();
			writer.Start(null);

			await ToJson(context, writer);

			var output = writer.ToUTF8String();

			// Release writer when fully done:
			writer.Release();

			return output;
		}

		/// <summary>
		/// Serialises the given context.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="writer">A stream to write the JSON string to.</param>
		/// <returns></returns>
		public async ValueTask ToJsonString(Context context, Writer writer)
		{
			await ToJson(context, writer);
		}

		/// <summary>
		/// The name of the cookie in use.
		/// </summary>
		public string ImpersonationCookieName
		{
			get
			{
				return Context.ImpersonationCookieName;
			}
		}

		/// <summary>
		/// The name of the cookie in use.
		/// </summary>
		public string CookieName
        {
            get
            {
                return _users.CookieName;
            }
        }

		/// <summary>
		/// Cookie domain
		/// </summary>
		private static string[] _domains = null;
		
		/// <summary>
		/// Cookie domain to use
		/// </summary>
		/// <returns></returns>
		public string GetDomain(uint localeId)
		{
			if (_domains == null)
			{
				_domains = new string[ContentTypes.Locales == null ? localeId : ContentTypes.Locales.Length];
			}

			if (localeId == 0 || localeId > 10000)
			{
				return null;
			}

			if (localeId > _domains.Length)
			{
				Array.Resize(ref _domains, (int)localeId);
			}

			var domain = _domains[localeId - 1];

			if (domain == null)
			{
				var cookieDomain = AppSettings.GetString("CookieDomain");
				if (cookieDomain != null)
				{
					domain = cookieDomain;
					_domains[localeId - 1] = domain;
				}
				else
				{
					#if DEBUG
						// Localhost - Can't use localhost:AppSettings.GetInt32("Port", 5000) because the websocket request would omit the cookie.
						return null;
					#else
					domain = AppSettings.GetPublicUrl(localeId).Replace("https://", "").Replace("http://", "");
					if (domain.StartsWith("www."))
					{
						domain = domain.Substring(4);
					}
					
					var fwdSlash = domain.IndexOf('/');
					
					if(fwdSlash != -1)
					{
						// Trim everything else off:
						domain = domain.Substring(0, fwdSlash);
					}

					_domains[localeId - 1] = domain;
					#endif
				}
			}

			return domain;
		}

		/// <summary>
		/// Gets a login token from the given cookie text.
		/// </summary>
		/// <param name="tokenStr"></param>
		/// <param name="ctx">Populates in to the given context object.</param>
		/// <param name="customKeyPair">Key pair to use when checking the signature. If null, this uses the internal one used by signature service.</param>
		/// <returns></returns>
		public async ValueTask<bool> Get(string tokenStr, Context ctx, KeyPair customKeyPair = null)
        {
            if (tokenStr == null)
            {
				return false;
            }

			// Token format is:
			// (1 digit) - Version
			// Y digits - Timestamp, in ms
			// N fields, where a field is:
			// (1 character) - Field identifier
			// (X digits) - the ID
			// (64 alphanum) - The hex encoded HMAC-SHA256 of everything before it.

			if (tokenStr.Length < 65 || tokenStr[0] != '1')
			{
				// Must start with version 1
				return false;
			}

			if (!_signatures.ValidateHmac256AlphaChar(tokenStr, customKeyPair))
			{
				return false;
			}

			var sigStart = tokenStr.Length - 64;

			uint currentId = 0;

			// If any field does not pass, we reject the whole thing.
			var i = 1;

			// Skip timestamp:
			while (i < sigStart)
			{
				var current = tokenStr[i];
				if (current >= 48 && current <= 57)
				{
					i++;
				}
				else
				{
					break;
				}
			}

			while (i < sigStart)
			{
				var fieldIndex = tokenStr[i] - 'A';

				if (fieldIndex < 0 || fieldIndex > 64)
				{
					// Invalid field index.
					return false;
				}

				var field = ContextFields.FieldsByShortcode[fieldIndex];

				if (field == null)
				{
					// Invalid field index.
					return false;
				}

				i++;

				// keep reading numbers until there aren't anymore:
				var current = tokenStr[i];

				while (current >= 48 && current <= 57 && i < sigStart)
				{
					i++;
					currentId = currentId * 10;
					currentId += (uint)(current - 48);
					current = tokenStr[i];
				}

				// Completed the ID for field at 'fieldIndex'.
				field.PrivateFieldInfo.SetValue(ctx, currentId);
				currentId = 0;
			}

			if (ctx.UserId != 0)
			{
				// Get the user row and apply now:
				ctx.User = await _users.Get(ctx, ctx.UserId, DataOptions.IgnorePermissions);
			}

			return true;
		}

		/// <summary>
		/// Creates a signed token for the given context.
		/// </summary>
		/// <param name="context">The context to create the token for.</param>
		/// <returns></returns>
		public string CreateToken(Context context)
		{
			var writer = Writer.GetPooled();
			writer.Start(VersionField);
			writer.WriteS(DateTime.UtcNow);

			foreach (var field in ContextFields.FieldList)
			{
				if (field.OmitFromToken)
				{
					continue;
				}

				var value = (uint)field.PrivateFieldInfo.GetValue(context);

				if (value == field.DefaultValue)
				{
					continue;
				}

				writer.Write((byte)field.Shortcode);
				writer.WriteS(value);
			}

			_signatures.SignHmac256AlphaChar(writer);
			var tokenStr = writer.ToASCIIString();
			writer.Release();
			return tokenStr;
        }

		/// <summary>
		/// Creates a token for accessing a remote site which permits access to the given hostname.
		/// The given keypair must contain the private key that we'll use, and the remote system must have the public key in its SignatureService Hosts config.
		/// </summary>
		/// <param name="context">The context to create the token for.</param>
		/// <param name="hostName">If provided, a hostname to use in the token. 
		/// You can define a lookup of remote public keys in your SignatureService config, 
		/// allowing third party systems to create valid tokens. This hostname is the key in that lookup.</param>
		/// <param name="keyPair">A keypair just for the purpose of accessing the remote host. 
		/// It must not be the same as the main keypair for this site.</param>
		/// <returns></returns>
		public string CreateRemoteToken(Context context, string hostName, KeyPair keyPair)
		{
			/*
			var builder = new StringBuilder();
			bool first = true;

			foreach (var field in FieldList)
			{
				var value = (int)field.PrivateFieldInfo.GetValue(context);

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
			tokenStr += "|" + keyPair.SignBase64(tokenStr) + "|" + hostName;

			return tokenStr;
			*/

			throw new NotImplementedException("Remote tokens are WIP.");
		}

    }
}
