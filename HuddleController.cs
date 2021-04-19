using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.Startup;
using Api.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Huddles
{
    /// <summary>Handles huddle endpoints.</summary>
    [Route("v1/huddle")]
	public partial class HuddleController : AutoController<Huddle>
    {

		/// <summary>
		/// Huddle server is informing about state updates of one or more users.
		/// </summary>
		[HttpPost("state")]
		public async ValueTask<object> State([FromBody] HuddleUserStates userStates)
		{
			var context = Request.GetContext();

			if (context == null)
			{
				return null;
			}

			// First, we'll need to ensure this request is coming from a legitimate server.
			// The URL contains a signature at the end, which is a signature of the rest of the URL including a UTC timestamp in seconds.
			// To ensure that this isn't just a stale URL that some evil proxy somehow captured (by breaking https between 2 servers), 
			// we must check that the UTC timestamp in seconds in the URL is recent. Do this first as it's the lightest on our resources:
			if (!Request.Query.TryGetValue("t", out StringValues value))
			{
				// No timestamp
				throw new PublicException("No timestamp", "timestamp_required");
			}

			if (!Request.Query.TryGetValue("sig", out StringValues sigValue))
			{
				// No signature
				throw new PublicException("No signature", "signature_required");
			}

			var signature = sigValue.ToString();

			if (!long.TryParse(value.ToString(), out long timestamp))
			{
				// Bad timestamp (not a number)
				throw new PublicException("Bad timestamp (unix timestamp in seconds)", "timestamp_invalid");
			}

			// Is it within ~5 minutes (5*60) of us? It can be +/-.
			var currentUnixTime = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

			if ((timestamp > (currentUnixTime + 300)) || (timestamp < (currentUnixTime -300)))
			{
				// Timestamp is too far away
				throw new PublicException("Timestamp out of range (unix timestamp in seconds)", "timestamp_invalid_range");
			}

			// Ok timestamp is recent enough - next attempt to verify the signature. The server signed the complete URL including http(s)://our hostname:
			var completeUrl = "http";

			// Using the same rule as the huddle server here - if it's localhost then http, otherwise s:
			var hostName = Request.Host.Value;

			if (!hostName.StartsWith("localhost:"))
			{
				completeUrl += "s";
			}

			completeUrl += "://" + hostName + Request.Path + Request.QueryString;

			// Split off the &sig=:
			var sigStart = completeUrl.IndexOf("&sig=");
			var signedUrl = completeUrl.Substring(0, sigStart);

			// Get the huddle server:
			var huddleServerService = Services.Get<HuddleServerService>();
			var huddleServer = await huddleServerService.Get(context, userStates.HuddleServerIdentifier, DataOptions.IgnorePermissions);

			// Check if the signature validates with the Huddle servers public key:
			if (huddleServer == null)
			{
				// Bad server identifier
				throw new PublicException("Incorrect huddleServerIdentifier in the payload (should be the Id of a huddleServer entry on this API).", "huddleserver_invalid");
			}

			if (string.IsNullOrEmpty(huddleServer.PublicKey))
			{
				// Not setup with PKI
				throw new PublicException("publicKey not set for that huddleServer entry.", "huddleserver_invalid_nokey");
			}

			// Got the key, got the signed data and the public key. Verify it now:
			var sigService = Services.Get<Signatures.SignatureService>();

			if (!sigService.ValidateSignature(signedUrl, signature, huddleServer.PublicKey))
			{
				throw new PublicException("Signature is invalid", "signature_invalid");
			}

			if (userStates != null && userStates.Users != null && userStates.Users.Count != 0)
			{
				var presenceService = Services.Get<HuddlePresenceService>();
				var userService = Services.Get<UserService>();
				var huddleService = Services.Get<HuddleService>();

				var inMeetingAndLastHuddleId = userService.GetChangeField("InMeeting").And("LastJoinedHuddleId");
				var usersInMeeting = huddleService.GetChangeField("UsersInMeeting");

				// Very commonly just one update in this.
				foreach (var userState in userStates.Users)
				{
					// Add or update a huddle presence record.

					// Get all existing presence records for this user:
					var thisUsersPresence = await presenceService.List(context, new Filter<HuddlePresence>().Equals("UserId", userState.UserId), DataOptions.IgnorePermissions);

					// Match on huddle peer ID:
					HuddlePresence existingRecord = null;

					foreach (var presence in thisUsersPresence)
					{

						if (presence.PeerId == userState.PeerId && presence.HuddleServerId == huddleServer.Id)
						{
							// Match!
							existingRecord = presence;
							break;
						}

					}

					var change = 0;

					if (userState.Joined)
					{
						// They're joining a meeting.
						// The record must exist.
						if (existingRecord == null)
						{
							var now = DateTime.UtcNow;
							await presenceService.Create(context, new HuddlePresence()
							{
								UserId = userState.UserId,
								HuddleId = userState.HuddleId,
								PeerId = userState.PeerId,
								HuddleServerId = huddleServer.Id,
								CreatedUtc = now,
								EditedUtc = now
							});

							change = 1;
						}

						// Update user busy flag if they weren't already in a meeting:
						var user = await userService.Get(context, userState.UserId, DataOptions.IgnorePermissions);

						if (user != null)
						{
							if(await userService.StartUpdate(context, user, DataOptions.IgnorePermissions)){
								user.InMeeting = true;
								user.LastJoinedHuddleId = userState.HuddleId;
								user.MarkChanged(inMeetingAndLastHuddleId);
								await userService.FinishUpdate(context, user);
							}
						}
					}
					else
					{
						// The record must not exist.
						if (existingRecord != null)
						{
							await presenceService.Delete(context, existingRecord, DataOptions.IgnorePermissions);
							change = -1;
						}

						if ((existingRecord != null && thisUsersPresence.Count == 1) || thisUsersPresence.Count == 0)
						{
							// Update user busy flag:
							var user = await userService.Get(context, userState.UserId, DataOptions.IgnorePermissions);

							if (user != null)
							{
								if (await userService.StartUpdate(context, user, DataOptions.IgnorePermissions))
								{
									user.InMeeting = false;
									user.LastJoinedHuddleId = 0;
									user.MarkChanged(inMeetingAndLastHuddleId);
									await userService.FinishUpdate(context, user);
								}
							}
						}
					}

					if (change != 0)
					{
						// Update the huddle - get # of users currently in it:
						var huddle = await huddleService.Get(context, userState.HuddleId, DataOptions.IgnorePermissions);

						if (await huddleService.StartUpdate(context, huddle, DataOptions.IgnorePermissions))
						{
							huddle.UsersInMeeting += change;
							huddle.MarkChanged(usersInMeeting);
							await huddleService.FinishUpdate(context, huddle);
						}
					}
				}

			}

			return new {
				ok = true
			};
		}

		/// <summary>
		/// Loads a huddle by the slug. 
		/// </summary>
		/// <param name="slug"></param>
		/// <returns></returns>
		[HttpGet("{slug}/load")]
		public async ValueTask<object> SlugLoad(string slug)
        {
			var context = Request.GetContext();

			if (context == null)
            {
				return null;
            }

			var service = (_service as HuddleService);

			// Get the huddle:
			var huddles = await service.List(context, new Filter<Huddle>().Equals("Slug", slug));

			if (huddles.Count < 1)
			{
				return null;
			}

			var huddle = huddles[0];

			// Is the huddle valid?
			if (huddle == null)
			{
				return null;
			}

			if (!service.IsPermitted(context, huddle))
			{
				return null;
			}

			return huddle;
		}


		/// <summary>
		/// Join a huddle using the slug. Provided the user is permitted, this returns the connection information.
		/// </summary>
		/// <param name="slug"></param>
		/// <returns></returns>
		[HttpGet("{slug}/slug/join")]
		public async ValueTask<object> Join(string slug)
        {
			var context = Request.GetContext();

			if (context == null)
			{
				return null;
			}

			var service = (_service as HuddleService);

			// Get the huddle:
			var huddles = await service.List(context, new Filter<Huddle>().Equals("Slug", slug));

			if (huddles.Count < 1)
            {
				return null;
            }

			var huddle = huddles[0];

			// Is the huddle valid?
			if(huddle == null)
            {
				return null;
            }

			if (!service.IsPermitted(context, huddle))
			{
				return null;
			}

			// Get site hostname:
			var hostName = Request.Host.Value;

			// Sign a join URL:
			var connectionUrl = await service.SignUrl(context, huddle, hostName);
			var canViewAdmin = context.Role != null && context.Role.CanViewAdmin;

			if (huddle.HuddleType == 4 && canViewAdmin)
			{
				// Audience huddle type, and we're admin. Return a list of all servers as well.

				return new
				{
					huddle,
					huddleRole = 1,
					connectionUrl,
					servers = Services.Get<HuddleServerService>().GetHostList()
				};

			}
			else
			{
				return new
				{
					huddle,
					huddleRole = (huddle.UserId == context.UserId || canViewAdmin) ? 1 : 4,
					connectionUrl
				};
			}
		}


		/// <summary>
		/// Join a huddle. Provided the user is permitted, this returns the connection information.
		/// </summary>
		[HttpGet("{id}/join")]
		public async ValueTask<object> Join(uint id)
		{
			var context = Request.GetContext();

			if (context == null)
			{
				return null;
			}
			
			var service = (_service as HuddleService);
			
			// Get the huddle:
			var huddle = await service.Get(context, id);
			
			if(huddle == null){
				// Doesn't exist or not permitted (the permission system internally checks huddle type and invites).
				return null;
			}

			// Is the current contextual user permitted to join?
			// Either it's open, or they must be on the invite list (don't have to specifically have accepted though):
			if (!service.IsPermitted(context, huddle))
			{
				return null;
			}
			
			// Get site hostname:
			var hostName = Request.Host.Value;
			
			// Sign a join URL:
			var connectionUrl = await service.SignUrl(context, huddle, hostName);
			var canViewAdmin = context.Role != null && context.Role.CanViewAdmin;
			
			if(huddle.HuddleType == 4 && canViewAdmin)
			{
				// Audience huddle type, and we're admin. Return a list of all servers as well.
				
				return new {
					huddle,
					huddleRole = 1,
					connectionUrl,
					servers = Services.Get<HuddleServerService>().GetHostList()
				};
				
			}else{
				return new {
					huddle,
					huddleRole = (huddle.UserId == context.UserId || canViewAdmin) ? 1 : 4,
					connectionUrl
				};
			}
		}
		
    }

	/// <summary>
	/// </summary>
	public class HuddleUserStates
	{
		/// <summary>
		/// </summary>
		public List<HuddleUserState> Users { get; set; }

		/// <summary>
		/// Our huddle server ID.
		/// </summary>
		public uint HuddleServerIdentifier { get; set; }
	}
	
	/// <summary>
	/// </summary>
	public class HuddleUserState
	{
		/// <summary>
		/// </summary>
		public uint UserId { get; set; }

		/// <summary>
		/// </summary>
		public uint HuddleId { get; set; }

		/// <summary>
		/// </summary>
		public int PeerId { get; set; }

		/// <summary>
		/// </summary>
		public bool Joined { get; set; }
	}

}