using Microsoft.AspNetCore.Mvc;

namespace Api.Forms
{
    /// <summary>Handles form endpoints.</summary>
    [Route("v1/form")]
	public partial class FormController : AutoController<Form>
    {
    }
}