using System;
using Api.Startup;
using Microsoft.AspNetCore.Builder;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Collections.Generic;

namespace Api.Swagger
{

	/// <summary>
	/// Listens for events to setup the development pack directory.
	/// </summary>
	[EventListener]
	public class EventListener
	{
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public EventListener()
		{
			var title = GetAssemblyAttribute<AssemblyTitleAttribute>().Title;

			WebServerStartupInfo.OnConfigureServices +=
				(IServiceCollection builder) =>
				{
					builder.AddEndpointsApiExplorer();
					builder.AddOpenApiDocument(config =>
					{
						config.DocumentName = title;
						config.Title = title;
						config.Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
					});
				};

			// Also hook up the configure app method:
			WebServerStartupInfo.OnConfigureApplication += (IApplicationBuilder app) => {

				app.UseOpenApi();
				app.UseSwaggerUi(config =>
				{
					config.DocumentTitle = title;
					config.Path = "/swagger";
					config.DocumentPath = "/swagger/{documentName}/swagger.json";
					config.DocExpansion = "list";
				});

			};

		}

		public static T GetAssemblyAttribute<T>() where T : Attribute
		{
			var thisAsm = typeof(EventListener).Assembly;

			object[] attributes = thisAsm.GetCustomAttributes(typeof(T), false);

			if (attributes.Length == 0)
				return null;

			return attributes.OfType<T>().SingleOrDefault();
		}
	}
}
