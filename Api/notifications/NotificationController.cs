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
		
		/// <summary>
		/// Clears a set of a user's notifications, based on ids.
		/// </summary>
		/// <param name="ids"></param>
		/// <returns></returns>
		[HttpPost("markViewed")]
		public async Task<object> MarkViewedByIds([FromBody] IdSetBody ids)
        {
			var ctx = await Request.GetContext();

			if(ctx == null || ctx.UserId == 0)
            {
				return null;
            }

			await (_service as NotificationService).MarkSetViewed(ctx, ctx.UserId, ids.Ids);

			return new {};
        }
    }

	/// <summary>
	/// Ids sent to be marked as view.
	/// </summary>
	public class IdSetBody
    {
		/// <summary>
		/// The array of notificaiton ids.
		/// </summary>
		public uint[] Ids;
    }
}