using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Reflection;
using System.Threading.Tasks;

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
            var title = SwaggerService.GetAssemblyAttribute<AssemblyTitleAttribute>().Title;
            var version = "v1"; // Assembly.GetExecutingAssembly().GetName().Version?.ToString();

            WebServerStartupInfo.OnConfigureServices +=
                (IServiceCollection builder) =>
                {
					builder.AddRouting();

					builder.AddEndpointsApiExplorer();
                    builder.AddSwaggerGen(options =>
                    {
                        options.SwaggerDoc(version, new OpenApiInfo { Title = title, Version = version });
                        options.DocumentFilter<SwaggerDocumentFilter>();
                    });
                };

            // Also hook up the after app configuration
            WebServerStartupInfo.OnConfigureApplication += (IApplicationBuilder app) =>
            {
                //restrict access to admin panel users 
                app.UseMiddleware<SwaggerAuthenticationMiddleware>();

                // if we get a 500 error from /swagger/v1/swagger.json
                // try moving the calls below directly into WebServerStartupInfo
                // as this appeared to improve/expose the errors to stdout when initially trying
                // to get this to work ...
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"{title} {version}");
                    c.SupportedSubmitMethods(new SubmitMethod[] { SubmitMethod.Get});
                    
                    // auto shows try it out when chosing an endpoint
                    //c.EnableTryItOutByDefault();
                });
            };
        }

        /// <summary>
        /// 
        /// </summary>
        public class SwaggerDocumentFilter : IDocumentFilter
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="swaggerDoc"></param>
            /// <param name="context"></param>
            public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
            {
                //call service so that we have dynamic config etc 
                var swaggerService = Services.Get<SwaggerService>();
                if (swaggerService != null)
                {
                    swaggerService.FilterDocuments(swaggerDoc, context);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public class SwaggerAuthenticationMiddleware
        {
            private readonly RequestDelegate _next;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="next"></param>
            public SwaggerAuthenticationMiddleware(RequestDelegate next)
            {
                _next = next;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="context"></param>
            /// <returns></returns>
            public async Task InvokeAsync(HttpContext context)
            {
                if (context.Request.Path.StartsWithSegments("/swagger"))
                {
                    var ctx = await context.Request.GetContext();
                    if (ctx == null || ctx.Role == null || !ctx.Role.CanViewAdmin)
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }
                }

                await _next(context);
            }
        }
    }
}

