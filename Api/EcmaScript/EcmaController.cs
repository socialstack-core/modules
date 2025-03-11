using Microsoft.AspNetCore.Mvc;

namespace Api.EcmaScript
{
    [Route("v1/ecma")]
    public partial class EcmaController : ControllerBase
    {
        [HttpGet("test")]
        public string TestResp()
        {
            return "Hello world";
        }
    }
}