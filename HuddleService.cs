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
using Api.Uploader;
using System.Net.Http;
using System.Text;
using Api.CanvasRenderer;

namespace Api.Huddles
{
	/// <summary>
	/// Handles huddles.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class HuddleService : AutoService<Huddle>
    {
		private HuddleConfig _config;
		private readonly SignatureService _signatures;
		private readonly HuddleServerService _huddleServerService;
		private readonly int userTypeId;
		private Random random;

		/// <summary>
		/// Custom display name function.
		/// </summary>
		public Func<User, string> OnGetDisplayName;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public HuddleService(SignatureService signatures, HuddleServerService huddleServerService) : base(Events.Huddle)
        {
			_signatures = signatures;
			_huddleServerService = huddleServerService;
			userTypeId = ContentTypes.GetId(typeof(User));
			_config = GetConfig<HuddleConfig>();

			var slugField = GetChangeField("Slug");
			
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
					var serverToUse = await _huddleServerService.Allocate(ctx, huddle.StartTimeUtc, huddle.EstimatedEndTimeUtc, loadFactor, huddle.RegionId);

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

				// Let's generate a unique slug for this huddle, if it didn't have one already (admin only)
				if (ctx.Role != null && ctx.Role.CanViewAdmin)
				{
					if (!string.IsNullOrEmpty(huddle.Slug))
					{
						// Halt
						return huddle;					
					}
				}

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

				if (huddle.HasChanged(slugField))
				{
					// Let's check that the slug they are using right now is unique, excluding itself in the check obviously.
					var isUnique = await IsUniqueHuddleSlug(ctx, huddle.Slug, huddle.Id);

					// Is it unique?
					if (!isUnique)
					{
						//Nope! We need to replace it with a unique one. They can remodify it again if they aren't happy with their newly generated slug.
						huddle.Slug = await GenerateUniqueHuddleSlug(ctx);
						huddle.MarkChanged(slugField);
					}
				}

				return huddle;
			});

			// Huddle Transcode:
			Events.Upload.AfterCreate.AddEventListener(async (Context context, Upload upload) => {

				if (_config.TranscodeUploads)
				{
					// Is this a transcode-able upload?

					// If video, request a transcode.
					if (upload.IsVideo)
					{
						await RequestTranscode(upload);
					}
				}

				return upload;
			});
		}

		/// <summary>
		/// Asks the huddle cluster to transcode the given upload.
		/// </summary>
		/// <param name="upload"></param>
		/// <returns></returns>
		public async Task RequestTranscode(Upload upload)
		{
			var sourceUrl = upload.GetUrl("original");

			// Get the callback url:
			var callbackUrl = upload.GetTranscodeCallbackUrl();

			var client = new HttpClient();

			var bodyJson = Newtonsoft.Json.JsonConvert.SerializeObject(new {
				sourceUrl = sourceUrl,
				callbackUrl = callbackUrl
			});

			var json = Newtonsoft.Json.JsonConvert.SerializeObject(new {
				header = new {
					signature = "todo"
				},
				body = bodyJson
			});

			var randomServer = _huddleServerService.RandomServer();

			if (randomServer == null)
			{
				// Unable to request a transcode.
				return;
			}

			string addr;

			if (randomServer.Address.StartsWith("localhost"))
			{
				// Local huddle server special case - it's the only one that can run on http.
				addr = "http://" + randomServer.Address;
			}
			else
			{
				addr = "https://" + randomServer.Address;
			}

			await client.PostAsync(addr + "/transcode", new StringContent(json, Encoding.UTF8, "application/json"));
		}

		private string slugPattern = "abcdefghjkmnpqrstuvwxyzACDEFHJKMNPRTUVWXY1234567890";

		/// <summary>
		/// Returns a huddle slug in the form of xxx-xxx-xxx alphanum excluding amigious characters 
		/// </summary>
		/// <returns></returns>
		public string GenerateHuddleSlug()
        {
			if(random == null)
            {
				random = new Random();
            }

			// Non-allocating random string (except for the actual string itself).

			int length = 11; // 3 segments of 3, plus 2 dashes.
			string slug = string.Create(length, random, (Span<char> chars, Random r) =>
			{
				for(var i=0;i<11;i++)
				{
					if (i == 3 || i == 7)
					{
						chars[i] = '-';
					}
					else
					{
						chars[i] = slugPattern[r.Next(slugPattern.Length)];
					}
				}
			});

			return slug;
        }

		/// <summary>
		/// Used to determine if a slug is unique.
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="slug"></param>
		/// <param name="exclusionId"></param>
		/// <returns></returns>
		public async ValueTask<bool> IsUniqueHuddleSlug(Context ctx, string slug, uint? exclusionId = null)
        {
			if (!exclusionId.HasValue)
			{
				return !await Where("Slug=?", DataOptions.IgnorePermissions).Bind(slug).Any(ctx);
			}

			return !await Where("Slug=? and Id!=?", DataOptions.IgnorePermissions).Bind(slug).Bind(exclusionId.Value).Any(ctx);
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

			while (!isUnique)
            {
				// Let's reroll and check again
				slug = GenerateHuddleSlug();
				isUnique = await IsUniqueHuddleSlug(ctx, slug);
			}

			return slug;
        }
		
		/// <summary>
		/// Creates a signed join URL for a self-test (only 1 user in the huddle, for the purposes of testing connectivity).
		/// </summary>
		public string SelfTestUrl(Context context)
		{
			var huddleServer = _huddleServerService.RandomServer();
			
			if (huddleServer == null)
			{
				return null;
			}
			
			var huddleId = context.UserId + "-" + DateTime.UtcNow.Ticks;
			
			var queryStr = "h=" + huddleId + "&u=" + context.UserId + "&d=&a=&type=0&urole=4&role=4";
			
			// This signature is what allows the user to fully authenticate on a db-less target server:
			var sig = _signatures.Sign(queryStr);
			
			return huddleServer.Address + "/?" + queryStr + "&sig=" + HttpUtility.UrlEncode(sig);
		}
		
		/// <summary>
		/// Creates a signed join URL.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="huddle"></param>
		/// <param name="localHostName">
		/// The hostname of "this" API. Usually just the public site hostname, "www.example.com".
		/// </param>
		public async ValueTask<string> SignUrl(Context context, Huddle huddle, string localHostName)
		{
			var user = context.User;

			var joinInfo = new HuddleJoinInfo()
			{
				DisplayName = OnGetDisplayName == null ? (user == null ? "Anonymous" : user.Username) : OnGetDisplayName(user),
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
				huddleServer = await _huddleServerService.Get(context, huddle.HuddleServerId, DataOptions.IgnorePermissions);
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
