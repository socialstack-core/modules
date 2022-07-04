using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Users;
using Api.Eventing;
using Api.Contexts;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Api.Startup;
using Newtonsoft.Json.Linq;
using Api.Signatures;
using System;

namespace Api.PublicKeyAuth
{
	/// <summary>
	/// Public key authentication scheme. The user's device has a private key which it uses to sign a challenge.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PublicKeyAuthService
    {
		private DateTime _epoch = new DateTime(1970, 1, 1);
		private UserService _users;
		private SignatureService _signatures;
		private Random _rng = new Random();
		
		/// <summary>
		/// Returns a unix timestamp.
		/// </summary>
		public long Timestamp()
		{
			return (long)(DateTime.UtcNow.Subtract(_epoch)).TotalSeconds;
		}
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PublicKeyAuthService(UserService users, SignatureService sigs)
        {
			_users = users;
			_signatures = sigs;

			// Hook up to the UserOnAuthenticate event:
			Events.UserOnAuthenticate.AddEventListener(async (Context context, LoginResult result, UserLogin loginDetails) => {

				if (result != null)
				{
					// Some other auth handler has already authed this user.
					return result;
				}

				// Email/ signature of a random challenge.
				if (string.IsNullOrEmpty(loginDetails.EmailAddress))
				{
					// This request probably isn't meant for us.
					return null;
				}
				
				// First, get the user by the email address:
				var user = await _users.Get(context, loginDetails.EmailAddress);
				
				if (user == null)
				{
					return null;
				}
				
				var keyToUse = user.PublicKey;
				
				if (string.IsNullOrEmpty(keyToUse))
				{
					// Disabled for this account
					return null;
				}
				
				if(string.IsNullOrEmpty(loginDetails.ChallengeResponse))
				{
					// Send a challenge now.
					// The client must return the whole challenge signed with :base64(DER signature) appended on the end.

					var buffer = new byte[8];
					_rng.NextBytes(buffer);

					var challenge = "pki:" + Timestamp() + ":" + System.Convert.ToBase64String(buffer);
					
					// Sign the above with the server signature so it can't be changed:
					var serverSignature = _signatures.Sign(challenge);

					// Return it as more detail required, starting with pki.
					// That triggers a sign on the client end, and a resend.
					return new LoginResult(){
						MoreDetailRequired = challenge + ":" + serverSignature
					};
				}
				
				// Validate the challenge response.
				var pieces = loginDetails.ChallengeResponse.Split(":");
				
				// Must be 5 sections - the above 4 from the challenge, plus the base64 signature.
				if(pieces.Length != 5 || pieces[0] != "pki")
				{
					return null;
				}
				
				// Timestamp:
				if(!long.TryParse(pieces[1], out long ts))
				{
					return null;
				}
				
				// If it was too long ago (>60s) reject.
				var currentTs = Timestamp();
				
				if((currentTs - ts) > 60)
				{
					return null;
				}
				
				// The text the server signed:
				var thingISigned = pieces[0] + ":" + pieces[1] + ":" + pieces[2];
				
				if(!_signatures.ValidateSignature(thingISigned, pieces[3]))
				{
					// Not a valid server signature. Somebody tampered the text (probably).
					return null;
				}
				
				// The thing they signed is..
				var thingTheySigned = thingISigned + ":" + pieces[3];
				
				// Does it match?
				/*
				if(_signature.ValidateSignature(thingTheySigned, pieces[4], keyToUse))
				{
					// Bad signature
					return null;
				}
				*/
				
				// Successful login - return the user:
				return new LoginResult() {
					User = user
				};
			}, 30);
			
		}

	}
	
}

namespace Api.Users {

	public partial class UserLogin {
		/// <summary>
		/// Email to use for PKA.
		/// </summary>
		public string EmailAddress;
		/// <summary>
		/// The challenge response.
		/// </summary>
		public string ChallengeResponse;
	}

	public partial class User {

		/// <summary>
		/// Seeded password hash
		/// </summary>
		[DatabaseField(Length = 100)]
		[JsonIgnore]
		public string PublicKey;

	}

}
