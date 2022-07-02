using Microsoft.AspNetCore.Mvc;

namespace Api.Views
{
    /// <summary>
    /// Handles view endpoints
    /// </summary>

    [Route("v1/view")]
	public partial class ViewController : AutoController<View>
    {
    }

}
