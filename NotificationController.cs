using Api.Contexts;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Notifications
{
    /// <summary>Handles notification endpoints.</summary>
    [Route("v1/notification")]
	public partial class NotificationController : AutoController<Notification>
    {
		/// <summary>
		/// Clears all of a users notifications, marking all as viewed.
		/// </summary>
		[HttpGet("clear")]
		public async Task<object> MarkAllViewed()
		{
			var ctx = await Request.GetContext();
			
			if(ctx == null || ctx.UserId == 0)
			{
				return null;
			}
			
			await (_service as NotificationService).MarkAllViewed(ctx, ctx.UserId);
			
			return new {};
		}
		
    }
}