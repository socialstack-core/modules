using Microsoft.AspNetCore.Mvc;

namespace Api.SmsMessages
{
    /// <summary>Handles sms template endpoints.</summary>
    [Route("v1/smstemplate")]
	public partial class SmsTemplateController : AutoController<SmsTemplate>
    {
    }
}