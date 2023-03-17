using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
	/// Holds useful information about Context objects.
	/// </summary>
	public static class ContextFields
	{

		/// <summary>
		/// Fields by the shortcode, which is usually the first character of a context field name.
		/// </summary>
		public static readonly ContextFieldInfo[] FieldsByShortcode = new ContextFieldInfo[64];

		/// <summary>
		/// Maps lowercase field names to the info about them.
		/// </summary>
		public static readonly Dictionary<string, ContextFieldInfo> Fields = new Dictionary<string, ContextFieldInfo>();

		/// <summary>
		/// The raw list of fields.
		/// </summary>
		public static readonly List<ContextFieldInfo> FieldList = new List<ContextFieldInfo>();

		/// <summary>
		/// Maps a content type ID to the context field info. Your context property must end with 'Id' to get an entry here.
		/// </summary>
		public static readonly Dictionary<int, ContextFieldInfo> ContentTypeToFieldInfo = new Dictionary<int, ContextFieldInfo>();


		static ContextFields()
		{
			// Load all the props now.
			var properties = typeof(Context).GetProperties(BindingFlags.Public | BindingFlags.Instance);
			var fields = typeof(Context).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

			var mapping = new Dictionary<string, FieldInfo>();

			foreach (var field in fields)
			{
				mapping[field.Name.ToLower()] = field;
			}

			var defaultValueChecker = new Context();

			foreach (var field in properties)
			{
				// must be a uint field.
				if (field.PropertyType != typeof(uint))
				{
					continue;
				}

				if (!field.Name.EndsWith("Id"))
				{
					continue;
				}

				var lcName = field.Name.ToLower();
				var getMethod = field.GetGetMethod();

				var defaultValue = (uint)getMethod.Invoke(defaultValueChecker, System.Array.Empty<object>());

				if (!mapping.TryGetValue("_" + lcName, out FieldInfo privateField))
				{
					throw new Exception(
						"For '" + field.Name + "' to be a valid Context property, it must have a private backing field called _" + lcName +
						". This is such that you can restrict setting your context field from anything other than a token or an object set."
					);
				}

				var fld = new ContextFieldInfo()
				{
					PrivateFieldInfo = privateField,
					Property = field,
					Name = field.Name,
					DefaultValue = defaultValue,
					SkipOutput = lcName == "roleid"
				};

				var shortCodeAttrib = field.GetCustomAttribute<ContextShortcodeAttribute>();

				var shortcode = shortCodeAttrib == null ? lcName[0] : shortCodeAttrib.Shortcode;

				var shortIndex = shortcode - 'A';

				if (shortIndex < 0 || shortIndex >= 64)
				{
					throw new Exception("Can't use " + shortcode + " as a context field shortcode - it must be A-Z or a-z.");
				}

				fld.Shortcode = shortcode;

				if (FieldsByShortcode[shortIndex] != null)
				{
					throw new Exception(
						"Context field '" + field.Name + "' can't use context field short name '" + shortcode +
						"' because it's in use. Specify one to use with [ContextShortcode('...')] on the public property.");
				}

				FieldsByShortcode[shortIndex] = fld;

				// E.g. UserId, LocaleId.
				// Get content type ID:
				var contentName = field.Name[0..^2];
				AutoService svc = null;

				//Hack for currencyLocale
				if (contentName == "CurrencyLocale")
                {
					var contentTypeId = ContentTypes.GetId("Locale");
					fld.ContentTypeId = contentTypeId;
					ContentTypeToFieldInfo[contentTypeId] = fld;

					svc = Services.GetByContentTypeId(contentTypeId);
				} 
				else
                {
					var contentTypeId = ContentTypes.GetId(contentName);
					fld.ContentTypeId = contentTypeId;
					ContentTypeToFieldInfo[contentTypeId] = fld;

					svc = Services.GetByContentTypeId(contentTypeId);
				}

				if (svc != null)
				{
					fld.ViewCapability = svc.GetEventGroup().GetLoadCapability();
				}

				var jsonHeader = "\"" + contentName.ToLower() + "\":";

				fld.JsonFieldHeader = Encoding.UTF8.GetBytes(jsonHeader);

				Fields[lcName] = fld;
				FieldList.Add(fld);
			}
		}
	}

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

				if (fld.Service == null)
				{
					fld.Service = Services.GetByContentTypeId(fld.ContentTypeId);
				}

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
					await fld.Service.OutputById(context, id, writer);
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
		/// Gets Context field info for the given contentType. Null if it doesn't exist in Context.
		/// </summary>
		public ContextFieldInfo FieldByContentType(int contentTypeId)
		{
			ContextFields.ContentTypeToFieldInfo.TryGetValue(contentTypeId, out ContextFieldInfo result);
			return result;
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
                return Context.CookieName;
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
						// Localhost - Can't use localhost:AppSettings.GetInt32("Port", 5000) because the websocket request would omit the cookie.
						return null;
					#else
					var domain = AppSettings.GetPublicUrl().Replace("https://", "").Replace("http://", "");
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
					
					_domain = domain;
					#endif
				}
			}

			return _domain;
		}

		/// <summary>
		/// Gets a login token from the given cookie text.
		/// </summary>
		/// <param name="tokenStr"></param>
		/// <param name="customKeyPair">Key pair to use when checking the signature. If null, this uses the internal one used by signature service.</param>
		/// <returns></returns>
		public async ValueTask<Context> Get(string tokenStr, KeyPair customKeyPair = null)
        {
            if (tokenStr == null)
            {
				return null;
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
				return null;
			}

			if (!_signatures.ValidateHmac256AlphaChar(tokenStr, customKeyPair))
			{
				return null;
			}

			var ctx = new Context();

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
					return null;
				}

				var field = ContextFields.FieldsByShortcode[fieldIndex];

				if (field == null)
				{
					// Invalid field index.
					return null;
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

			return ctx;
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
				if (field.SkipOutput)
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
