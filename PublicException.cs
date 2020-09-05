using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;


namespace Api.Startup
{
    /// <summary>
    /// Used when an exception is ok to be displayed publicly. It's outputted as an ErrorResponse.
    /// </summary>
    public class PublicException : Exception
    {
		/// <summary>
		/// Undelying error response.
		/// </summary>
        public ErrorResponse Response;
		
		/// <summary>
		/// Status code. Usually 400.
		/// </summary>
		public int StatusCode;
		
		
		/// <summary>
		/// Make a new exception. Throw it when doing this.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="code"></param>
		/// <param name="statusCode"></param>
		public PublicException(string message, string code = null, int statusCode = 400) : base(message)
		{
			StatusCode = statusCode;
			Response = new ErrorResponse()
			{
				Message = message,
				Code = code
			};
		}

		/// <summary>
		/// Serializes the error response to JSON.
		/// </summary>
		/// <returns></returns>
		public string ToJson()
		{
			return JsonConvert.SerializeObject(Response);
		}

		/// <summary>
		/// Outputs to the given HttpResponse.
		/// </summary>
		public virtual ErrorResponse Apply(HttpResponse response)
		{
			Console.WriteLine(Response.Message);
			response.StatusCode = StatusCode;
			return Response;
		}
    }
}
