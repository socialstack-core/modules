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

namespace Api.Huddles
{
	/// <summary>
	/// Handles huddles.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class HuddleService : AutoService<Huddle>
    {
		private SignatureService _signatures;
		private HuddleServerService _huddleServerService;
		private int userTypeId;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public HuddleService(SignatureService signatures, HuddleServerService huddleServerService) : base(Events.Huddle)
        {
			_signatures = signatures;
			_huddleServerService = huddleServerService;
			userTypeId = ContentTypes.GetId(typeof(User));

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
					throw new PublicException(
						"We're a little busy at the moment and have ran out of available meeting servers. " +
						"Please try again shortly, or contact support if this keeps occuring.", "no_huddle_servers"
					);
				}

				huddle.HuddleServerId = serverToUse.Id;
				return huddle;
			}, 5);
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

			if (huddle.HuddleType == 0)
			{
				// Public open
				return true;
			}

			if (huddle.Invites == null)
			{
				return false;
			}

			if (context.UserId == context.UserId)
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
		public async Task<string> SignUrl(Context context, Huddle huddle)
		{
			var user = await context.GetUser();


			var joinInfo = new HuddleJoinInfo()
			{
				DisplayName = user == null ? "Anonymous" : user.Username,
				AvatarRef = user == null ? (string)null : user.AvatarRef
			};

			// Dispatch the get join info evt:
			joinInfo = await Events.HuddleGetJoinInfo.Dispatch(context, joinInfo, huddle, user);

			var queryStr = "h=" + huddle.Id + 
			"&u=" + context.UserId + 
			"&d=" + HttpUtility.UrlEncode(joinInfo.DisplayName) + 
			"&a=" + HttpUtility.UrlEncode(joinInfo.AvatarRef) +
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
