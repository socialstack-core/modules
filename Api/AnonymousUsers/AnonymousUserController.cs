using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.AnonymousUsers
{
    /// <summary>Handles anonymous user endpoints.</summary>
    [Route("v1/anonymoususer")]
    public partial class AnonymousUserController : Controller
    {

        /// <summary>
        /// Return the existing account or create a new 'anonymous' one
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [HttpGet("ensureaccount")]
        public async ValueTask<Context> EnsureAccount(Context context)
        {
            if(context.User == null)
            {
                context.User = await Services.Get<AnonymousUserService>().CreateAccount(context);
            }
            
            return context;
        }
    }
}