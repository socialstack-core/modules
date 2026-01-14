using Microsoft.AspNetCore.Mvc;

namespace Api.Counters
{
    /// <summary>Handles counter endpoints.</summary>
    [Route("v1/counter")]
	public partial class CounterController : AutoController<Counter>
    {
    }
}