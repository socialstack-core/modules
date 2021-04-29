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
        /// <summary>
        /// Creates a context
        /// </summary>
        /// <param name="loginInfo"></param>
        /// <returns></returns>
        [HttpPost("login")]
        public async ValueTask Login([FromBody] TempLogin loginInfo)
        {
            var context = await Request.GetContext();

            var result = await (_service as TemporaryGuestService).Login(context, loginInfo);

            if (!result.Success)
            {
                Response.StatusCode = 400;
                return;
            }

            await OutputContext(context);
        }
    }
}
