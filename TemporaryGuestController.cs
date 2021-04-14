using System.Threading.Tasks;
using Api.Contexts;
using Api.Startup;
using Api.Users;
using Microsoft.AspNetCore.Mvc;

namespace Api.TemporaryGuests
{

    /// <summary>
    /// </summary>
    public class TempLogin
    {
        /// <summary>
        /// </summary>
        public string Email;

        /// <summary>
        /// </summary>
        public string Token;
    }

    /// <summary>
    /// Controller for handling temporary guests
    /// </summary>
    [Route("v1/temporaryguest")]
    public partial class TemporaryGuestController : AutoController<TemporaryGuest>
    {
        [HttpPost("login")]
        public async Task<object> Login([FromBody] TempLogin loginInfo)
        {
            var context = Request.GetContext();

            var result = await (_service as TemporaryGuestService).Login(context, loginInfo);

            if (!result.Success)
            {
                Response.StatusCode = 400;
                return null;
            }

            context.SendToken(Response);

            return await context.GetPublicContext();
        }
    }
}
