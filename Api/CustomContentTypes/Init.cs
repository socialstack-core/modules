using System;
using Api.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Api.Eventing;
using Api.Contexts;
using System.Threading.Tasks;
using Api.Permissions;
using Api.Database;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Api.Users;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Api.CustomContentTypes
{

	/// <summary>
	/// Hooks in to the Kestrel config process such that it can add endpoints for custom content types.
	/// </summary>
	[EventListener]
	public class Init
	{

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public Init()
		{

			WebServerStartupInfo.OnConfigureServices += (IServiceCollection services) => {

				services.AddSingleton<IActionDescriptorChangeProvider>(ActionDescriptorChangeProvider.Instance);
				
				WebServerStartupInfo.mvcBuilder.ConfigureApplicationPartManager(m => {

					// Add the custom type provider which can add custom controllers at runtime into MVC:
					m.FeatureProviders.Add(new CustomTypeFeatureProvider());

				});

			};

		}
	}
}
