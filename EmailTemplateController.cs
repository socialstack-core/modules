using Microsoft.AspNetCore.Mvc;

namespace Api.Emails
{
    /// <summary>Handles emailTemplate endpoints.</summary>
    [Route("v1/emailTemplate")]
	public partial class EmailTemplateController : AutoController<EmailTemplate>
    {
    }
}