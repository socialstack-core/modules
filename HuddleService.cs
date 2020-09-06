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

namespace Api.Huddles
{
	/// <summary>
	/// Handles huddles.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class HuddleService : AutoService<Huddle>, IHuddleService
    {
		private ISignatureService _signatures;
		private IHuddleServerService _huddleServerService;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public HuddleService(ISignatureService signatures, IHuddleServerService huddleServerService) : base(Events.Huddle)
        {
			_signatures = signatures;
			_huddleServerService = huddleServerService;


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

				// Assign a server now.
				var serverToUse = await _huddleServerService.Allocate(ctx, huddle.StartTimeUtc, huddle.EstimatedEndTimeUtc, loadFactor);

				if (serverToUse == null)
				{
					// Failed to allocate (because there's no huddle servers setup, or because the service hasn't started yet).
					return null;
				}

				huddle.HuddleServerId = serverToUse.Id;
				return huddle;
			}, 5);
		}
		
		/// <summary>
		/// Creates a signed join URL.
		/// </summary>
		public async Task<string> SignUrl(Context context, Huddle huddle)
		{
			var user = await context.GetUser();

			string displayName = user == null ? "Anonymous" : user.Username;
			string avatarRef = user == null ? (string)null : user.AvatarRef;

			var queryStr = "h=" + huddle.Id + 
			"&u=" + context.UserId + 
			"&d=" + HttpUtility.UrlEncode(displayName) + 
			"&a=" + HttpUtility.UrlEncode(avatarRef) +
			"&type=" + huddle.HuddleType + 
			"&role=" + ((huddle.UserId == context.UserId) ? "1" : "4");
			
			// This signature is what allows the user to fully authenticate on a db-less target server:
			var sig = _signatures.Sign(queryStr);

			var huddleServer = await _huddleServerService.Get(context, huddle.HuddleServerId);

			if (huddleServer == null)
			{
				return null;
			}

			return huddleServer.Address + "/?" + queryStr + "&sig=" + HttpUtility.UrlEncode(sig);
		}
	}
    
}
