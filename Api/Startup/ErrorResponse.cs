using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Api.Startup
{
    /// <summary>
    /// Used when responding with an error
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// The error message
        /// </summary>
		public string message;
		
        /// <summary>
        /// Optional textual error code for localisation. E.g. "already_booked".
        /// </summary>
		public string code;
		
        /// <summary>
        /// The error message
        /// </summary>
		[JsonIgnore]
        public string Message{
			get{
				return message;
			}
			set{
				message = value;
			}
		}
		
        /// <summary>
        /// Optional textual error code for localisation. E.g. "already_booked".
        /// </summary>
		[JsonIgnore]
        public string Code{
			get{
				return code;
			}
			set{
				code = value;
			}
		}
    }
}
