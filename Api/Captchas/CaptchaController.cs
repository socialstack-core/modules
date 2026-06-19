using System.Threading.Tasks;
using Api.Contexts;
using Api.Startup;
using Api.Users;
using Microsoft.AspNetCore.Mvc;

namespace Api.Captchas
{
    /// <summary>Handles captcha endpoints.</summary>
    [Route("v1/captcha")]
	public partial class CaptchaController : AutoController<Captcha>
    {
        /// <summary>
        /// Exposes a random Captcha
        /// </summary>
        [HttpGet("random")]
        public async ValueTask<Captcha> Random(Context context)
        {
            var captcha = await Services.Get<CaptchaService>().Random(context);
            return captcha;
        }

        /// <summary>
        /// Checks a captcha response
        /// </summary>
        [HttpGet("check/{captchaId}")]
        public async Task<bool> Check(Context context, [FromRoute] uint captchaId, [FromQuery] string tag)
        {
            var success = await Services.Get<CaptchaService>().Check(context, captchaId, tag);
            return success;
        }
    }
}