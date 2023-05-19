using System;
using System.IO;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Eventing;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Api.Startup
{
	/// <summary>
	/// Handles exceptions being logged to a log service.
	/// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
		

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		/// <param name="next"></param>
		public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }
		
		/// <summary>
		/// Logs an exception and responds to the original requester.
		/// </summary>
		private async ValueTask HandleError(Exception e, HttpContext context, int statusCode)
		{
			byte[] responseBody;

			// Request URL:
			var basePath = context.Request.Path.ToString();
			var path = context.Request.Method + " " + basePath;
			if (context.Request.QueryString.HasValue)
			{
				path += "?" + context.Request.QueryString;
			}

			var tag = "";

			if (basePath.StartsWith("/v1/"))
			{
				var nextFwd = basePath.IndexOf('/', 4);

				if (nextFwd == -1)
				{
					tag = basePath.Substring(4);
				}
				else
				{
					tag = basePath.Substring(4, nextFwd - 4);
				}
			}

			if (statusCode >= 500){

				// If basepath starts with /v1/ then the tag will be the controller name.
				Log.Error(tag, e, "Application error on " + path);
				
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
				Log.Info(tag, e, "Access denied on " + path);
				
				responseBody = Encoding.UTF8.GetBytes(
					JsonConvert.SerializeObject(
						new ErrorResponse() {
							Message = "Access denied"
						}
					)
				);
			}
			
			await context.Response.Body.WriteAsync(responseBody, 0, responseBody.Length);
			context.Response.StatusCode = statusCode;
			context.Response.ContentType = "application/json";
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
