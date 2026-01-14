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
        public PublicMessage Response;
		
		/// <summary>
		/// Status code. Usually 400.
		/// </summary>
		public int StatusCode;
		
		
		/// <summary>
		/// Make a new exception. Throw it when doing this.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="code">Used to translate your message.</param>
		/// <param name="statusCode"></param>
		public PublicException(string message, string code, int statusCode = 400) : base(message)
		{
			StatusCode = statusCode;
			Response = new PublicMessage(message, code);
		}

		/// <summary>
		/// Serializes the error response to JSON.
		/// </summary>
		/// <returns></returns>
		public string ToJson()
		{
			return JsonConvert.SerializeObject(Response);
		}

    }

	/// <summary>
	/// Like PublicException, except it just declares a more generic success or informational message.
	/// </summary>
	public struct PublicMessage
	{
		/// <summary>
		/// The error message
		/// </summary>
		public string Message;

		/// <summary>
		/// Optional textual error code for localisation. E.g. "already_booked".
		/// </summary>
		public string Code;

		/// <summary>
		/// Create a new public message.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="code"></param>
		public PublicMessage(string message, string code)
		{
			Message = message;
			Code = code;
		}

	}
}
