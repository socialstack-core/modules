using System;
using System.IO;
using System.Security;
using System.Text;
using System.Threading.Tasks;
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

        private readonly ILogger _logger;
		
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		/// <param name="next"></param>
		/// <param name="logger"></param>
        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

		/// <summary>
		/// Runs during each request.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            byte[] responseBody = null;
            Exception anyE = null;
            int statusCode = 200;

            try
            {
                await _next(context);
            }
            catch (SecurityException secEx)
            {
                anyE = secEx;
                responseBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new ErrorResponse() { Message = "Access Denied" }));
                statusCode = 403;
            }
            catch (Exception e)
            {
                anyE = e;
                responseBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new ErrorResponse { Message ="Application error occured which has been logged" } ));
                statusCode = 500;
            }

            if (anyE != null && _logger != null)
            {
                _logger.LogCritical(anyE.ToString());

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
        }
    }
}
