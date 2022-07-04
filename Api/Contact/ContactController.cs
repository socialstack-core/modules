using Microsoft.AspNetCore.Mvc;


namespace Api.Contacts
{
	/// <summary>
	/// Handles contact endpoints.
	/// </summary>
	[Route("v1/contact")]
	public partial class ArticleController : AutoController<Contact>
	{
    }
}