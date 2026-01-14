using System.Threading.Tasks;
using Api.Contexts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Templates
{
    /// <summary>
    /// Handles template endpoints.
    /// </summary>

    [Route("v1/template")]
	public partial class TemplateController : AutoController<Template>
    {
		/// <summary>
		/// Gets a template by its given key.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="key"></param>
		/// <returns></returns>
	    [HttpGet("/by-key/{key}")]
	    public async ValueTask<Template> GetByKey(Context context, [FromRoute] string key)
	    {
		    return await _service.Where("Key = ?").Bind(key).First(context);
	    }
    }
}