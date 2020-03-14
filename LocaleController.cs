using System;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Translate
{
    /// <summary>
    /// Handles locale endpoints.
    /// </summary>

    [Route("v1/locale")]
	public partial class LocaleController : AutoController<Locale, LocaleAutoForm>
	{
    }

}
