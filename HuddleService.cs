using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using System;
using System.Net;
using System.Net.Sockets;
using Api.Signatures;
using System.Web;
using Api.Users;
using Api.Startup;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Api.Huddles
{
	/// <summary>
	/// Handles huddles.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class HuddleService : AutoService<Huddle>
    {
		private readonly SignatureService _signatures;
		private readonly HuddleServerService _huddleServerService;
		private readonly int userTypeId;
		private Random random;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public HuddleService(SignatureService signatures, HuddleServerService huddleServerService) : base(Events.Huddle)
        {
			_signatures = signatures;
			_huddleServerService = huddleServerService;
			userTypeId = ContentTypes.GetId(typeof(User));
			
			Events.HuddlePermittedUser.Listed.AddEventListener(async (Context context, List<HuddlePermittedUser> list, HttpResponse response) => {

				// If specifically using huddle permitted user list, add huddle objects to it:
				if (list == null || list.Count == 0)
				{
					return list;
				}

				// Get huddle list:
				var huddles = await List(context, new Filter<Huddle>().Id(list.Select(e => e.HuddleId)));

				var huddleLookup = new Dictionary<int, Huddle>();

				foreach (var huddle in huddles)
				{
					huddleLookup[huddle.Id] = huddle;
				}

				foreach (var entry in list)
				{
					huddleLookup.TryGetValue(entry.HuddleId, out Huddle huddle);
					if (huddle == null)
					{
						entry.HuddleMeta = null;
					}
					else
					{
						entry.HuddleMeta = new HuddleMeta()
						{
							Title = huddle.Title,
							StartTimeUtc = huddle.StartTimeUtc,
							EstimatedEndTimeUtc = huddle.EstimatedEndTimeUtc,
						};
					}
				}

				return list;
				
			});
			
			Events.Huddle.BeforeCreate.AddEventListener(async (Context ctx, Huddle huddle) =>
			{
				if (huddle.EstimatedParticipants <= 0) {
					// 2 is assumed:
					huddle.EstimatedParticipants = 2;
				}

				if (huddle.StartTimeUtc == DateTime.MinValue)
				{
					huddle.StartTimeUtc = DateTime.UtcNow;
				}

				if (huddle.EstimatedEndTimeUtc == DateTime.MinValue)
				{
					huddle.EstimatedEndTimeUtc = huddle.StartTimeUtc.AddHours(1);
				}

				var n = huddle.EstimatedParticipants;
				var loadFactor = n + (n * (n - 1));
				
				// Audience huddles don't have an assigned server.
				if(huddle.HuddleType != 4)
				{
					// Assign a server now.
					var serverToUse = await _huddleServerService.Allocate(ctx, huddle.StartTimeUtc, huddle.EstimatedEndTimeUtc, loadFactor);

					if (serverToUse == null)
					{
						// Failed to allocate (because there's no huddle servers setup, or because the service hasn't started yet).
						throw new PublicException(
							"We're a little busy at the moment and have ran out of available meeting servers. " +
							"Please try again shortly, or contact support if this keeps occuring.", "no_huddle_servers"
						);
					}
					
					huddle.HuddleServerId = serverToUse.Id;
				}
				
				return huddle;
			}, 5);

			// We need to generate the huddle's slug. 
			Events.Huddle.BeforeCreate.AddEventListener(async (Context ctx, Huddle huddle) =>
			{
				if (ctx == null || huddle == null)
                {
					return null;
                }

				// Let's generate a unique slug for this huddle.
				huddle.Slug = await GenerateUniqueHuddleSlug(ctx);

				return huddle;
			});

			// We need to handle some work on before update as well to prevent a case where we get duplicate slugs.
			Events.Huddle.BeforeUpdate.AddEventListener(async (Context ctx, Huddle huddle) =>
			{
				if (ctx == null || huddle == null)
                {
					return null;
                }

				// Let's check that the slug they are using right now is unique, excluding itself in the check obviously.
				var isUnique = await IsUniqueHuddleSlug(ctx, huddle.Slug, huddle.Id);

				// Is it unique?
				if (!isUnique)
                {
					//Nope! We need to replace it with a unique one. They can remodify it again if they aren't happy with their newly generated slug.
					huddle.Slug = await GenerateUniqueHuddleSlug(ctx);
                }

				return huddle;
			});
		}

		/// <summary>
		/// Returns a huddle slug in the form of xxx-xxx-xxx alphanum excluding amigious characters 
		/// </summary>
		/// <returns></returns>
		public string GenerateHuddleSlug()
        {
			var chars = "abcdefghjkmnpqrstuvwxyzACDEFHJKMNPRTUVWXY1234567890";
			var slug = "";

			if(random == null)
            {
				random = new Random();
            }

			var i = 0;
			while (i < 8)
            {
				slug += chars[random.Next(chars.Length)];

				if ((i + 1) % 3 == 0 && i + 1 != 9)
                {
					// add a dash
					slug += "-";
                }
            }

			return slug;
        }

		/// <summary>
		/// Used to determine if a slug is unique.
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="slug"></param>
		/// <returns></returns>
		public async ValueTask<bool> IsUniqueHuddleSlug(Context ctx, string slug, int? exclusionId = null)
        {
			var huddles = await List(ctx, new Filter<Huddle>().Equals("Slug", slug));

			return huddles.Count == 0;
		}


		/// <summary>
		/// Used to generate a unique huddle slug.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<string> GenerateUniqueHuddleSlug(Context ctx)
        {
			// Let's generate a slug
			var slug = GenerateHuddleSlug();

			// Is this slug unique?
			var isUnique = await IsUniqueHuddleSlug(ctx, slug);

			while (isUnique)
            {
				// Let's reroll and check again
				slug = GenerateHuddleSlug();
				isUnique = await IsUniqueHuddleSlug(ctx, slug);
			}

			return slug;
        }

		/// <summary>
		/// True if user is permitted to access the given huddle.
		/// </summary>
		/// <returns></returns>
		public bool IsPermitted(Context context, Huddle huddle)
		{
			if (huddle == null)
			{
				return false;
			}

			if (huddle.HuddleType == 0 || huddle.HuddleType == 4)
			{
				// Public open
				return true;
			}

			if (huddle.Invites == null)
			{
				return false;
			}

			if (context.UserId == huddle.UserId)
			{
				return true;
			}

			foreach (var invite in huddle.Invites)
			{
				if (invite.PermittedUserId != 0 && invite.PermittedUserId == context.UserId)
				{
					return true;
				}
				else if (invite.PermittedUserId == 0 && invite.InvitedContentTypeId == userTypeId && invite.InvitedContentId == context.UserId)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Creates a signed join URL.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="huddle"></param>
		/// <param name="localHostName">
		/// The hostname of "this" API. Usually just the public site hostname, "www.example.com".
		/// </param>
		public async Task<string> SignUrl(Context context, Huddle huddle, string localHostName)
		{
			var user = await context.GetUser();


			var joinInfo = new HuddleJoinInfo()
			{
				DisplayName = user == null ? "Anonymous" : user.Username,
				AvatarRef = user == null ? (string)null : user.AvatarRef
			};

			// Dispatch the get join info evt:
			joinInfo = await Events.HuddleGetJoinInfo.Dispatch(context, joinInfo, huddle, user);

			HuddleServer huddleServer;

			if (huddle.HuddleType == 4)
			{
				// Audience type - use a random server:
				huddleServer = _huddleServerService.RandomServer();
			}
			else
			{
				huddleServer = await _huddleServerService.Get(context, huddle.HuddleServerId);
			}

			if (huddleServer == null)
			{
				return null;
			}
			
			var queryStr = "h=" + huddle.Id +
			"&u=" + context.UserId +
			"&d=" + HttpUtility.UrlEncode(joinInfo.DisplayName) +
			"&a=" + HttpUtility.UrlEncode(joinInfo.AvatarRef) +
			"&type=" + huddle.HuddleType +
			"&urole=" + context.RoleId +
			"&role=" + ((huddle.UserId == context.UserId) ? "1" : "4");

			if (!string.IsNullOrEmpty(huddleServer.PublicKey))
			{
				// We have a public key, so we can use the callback (cb) webhook that informs about the state of users joining and exiting Huddles.
				// When the webhook runs, the server will sign the URL using its private key, and declare itself using the huddle server ID that we give it here.
				// This way we can map it back to a suitable public key (the server must not declare the public key during this webhook call).
				queryStr += "&cb=" + localHostName +
				"&hsid=" + huddle.HuddleServerId;
			}

			// This signature is what allows the user to fully authenticate on a db-less target server:
			var sig = _signatures.Sign(queryStr);
			
			return huddleServer.Address + "/?" + queryStr + "&sig=" + HttpUtility.UrlEncode(sig);
		}
	}
    
}
