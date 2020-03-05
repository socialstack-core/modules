using Microsoft.AspNetCore.Mvc;


namespace Api.IfAThenB
{
    /// <summary>
    /// Handles a then b endpoints.
    /// </summary>

    [Route("v1/athenb")]
	public partial class AThenBController : AutoController<AThenB, AThenBAutoForm>
    {
		
    }

}
