using System;
using System.IO;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Eventing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Api.Startup
{
	/// <summary>
	/// Handles exceptions being logged to a log service.
	/// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
		private static object msgLock = new object();
		

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		/// <param name="next"></param>
		public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;

			// Default console logging handler (removable):
			Events.Logging.AddEventListener((Context ctx, Logging l) =>
			{
				switch (l.LogLevel) {
					case LOG_LEVEL.Debug:
						Console.ForegroundColor = ConsoleColor.Green;
						Console.Write("[DEBUG] ");
						Console.ForegroundColor = ConsoleColor.White;
					break;
					case LOG_LEVEL.Error:
						Console.ForegroundColor = ConsoleColor.Red;
						Console.Write("[!] ");
						Console.ForegroundColor = ConsoleColor.White;
					break;
					case LOG_LEVEL.Information:
						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.Write("[INFO] ");
						Console.ForegroundColor = ConsoleColor.White;
					break;
					case LOG_LEVEL.Warning:
						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.Write("[WARN] ");
						Console.ForegroundColor = ConsoleColor.White;
					break;
				}

				if (l.Message != null)
				{
					Console.WriteLine(l.Message);
				}

				if (l.Exception != null)
				{
					// Special case: Don't log an "Info" SecurityException.
					if (l.LogLevel != LOG_LEVEL.Information || !(typeof(SecurityException).IsAssignableFrom(l.Exception.GetType())))
					{
						Console.WriteLine(l.Exception.ToString());
					}
				}

				return Task.FromResult(l);
			});

        }
		
		/// <summary>
		/// Logs an exception and responds to the original requester.
		/// </summary>
		private async Task HandleError(Exception e, HttpContext context, int statusCode)
		{
			byte[] responseBody = null;
			var ctx = context.Request.GetContext();

			// Request URL:
			var path = context.Request.Method + " " + context.Request.Path;
			if (context.Request.QueryString != null && context.Request.QueryString.HasValue)
			{
				path += "?" + context.Request.QueryString;
			}
			
			if (statusCode >= 500){

				await Events.Logging.Dispatch(ctx, new Logging()
				{
					Exception = e,
					LogLevel = LOG_LEVEL.Error,
					Message = "Application error on " + path
				});
				
				// Internal server error
				responseBody = Encoding.UTF8.GetBytes(
					JsonConvert.SerializeObject(
						new ErrorResponse {
							Message ="Application error occured which has been logged"
						}
					)
				);
				
			}else{
				// Access denied:
				await Events.Logging.Dispatch(ctx, new Logging()
				{
					Exception = e,
					LogLevel = LOG_LEVEL.Information,
					Message = "Access denied on " + path + " " + e.Message
				});
				
				responseBody = Encoding.UTF8.GetBytes(
					JsonConvert.SerializeObject(
						new ErrorResponse() {
							Message = "Access denied"
						}
					)
				);
			}
			
			using (var ms = new MemoryStream())
			{
				ms.Seek(0, SeekOrigin.Begin);
				await ms.WriteAsync(responseBody, 0, responseBody.Length);
				ms.Seek(0, SeekOrigin.Begin);
				context.Response.StatusCode = statusCode;
				context.Response.ContentType = "application/json";
				await ms.CopyToAsync(context.Response.Body);
			}
		}
		
		/// <summary>
		/// Runs during each request.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (SecurityException secEx)
            {
				await HandleError(secEx, context, 403);
            }
            catch (Exception e)
            {
                await HandleError(e, context, 500);
				
				#if DEBUG
				// Rethrow for easy debugging:
				throw;
				#endif
            }
        }
    }
}
