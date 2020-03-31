using Microsoft.AspNetCore.Mvc;


namespace Api.Faqs
{
	/// <summary>
	/// Handles faq endpoints.
	/// </summary>
	[Route("v1/faq")]
	public partial class FaqController : AutoController<Faq>
	{
    }
}