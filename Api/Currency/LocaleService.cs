using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;
using System;
using Api.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Linq;

namespace Api.Translate
{
	/// <summary>
	/// Handles locales - the core of the translation (localisation) system.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class LocaleService
    {
		/// <summary>
		/// The name of the cookie when locale is stored.
		/// </summary>
		public string CurrencyLocaleCookieName => "CurrencyLocale";
	}
    
}
