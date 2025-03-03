using Microsoft.AspNetCore.Mvc;

namespace Api.Layouts
{
    /// <summary>Handles layout endpoints.</summary>
    [Route("v1/layout")]
	public partial class LayoutController : AutoController<Layout>
    {
    }
}