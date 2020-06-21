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
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public HuddleService(ISignatureService signatures) : base(Events.Huddle)
        {
			_signatures = signatures;
		}
		
		/// <summary>
		/// Creates a signed join URL.
		/// </summary>
		public async Task<string> SignUrl(Context context, Huddle huddle)
		{
			var user = await context.GetUser();

			string displayName = user == null ? "Anonymous" : user.Username;

			var queryStr = "h=" + huddle.Id + "&u=" + context.UserId + "&d=" + HttpUtility.UrlEncode(displayName) + "&a=" + HttpUtility.UrlEncode(user.AvatarRef);
			
			// This signature is what allows the user to fully authenticate on a db-less target server:
			var sig = _signatures.Sign(queryStr);
			
			return huddle.ServerAddress + "/?" + queryStr + "&sig=" + HttpUtility.UrlEncode(sig);
		}
	}
    
}
