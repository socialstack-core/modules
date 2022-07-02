using Microsoft.AspNetCore.Mvc;

namespace Api.Persons
{
    /// <summary>
    /// Handles people endpoints.
    /// </summary>

    [Route("v1/person")]
	public partial class PersonController : AutoController<Person, PersonAutoForm>
    {
    }
}