using System;
using Api.Permissions;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;

namespace Api.Eventing
{
	/// <summary>
	/// The level of a particular logged message.
	/// </summary>
    public enum LOG_LEVEL
    {
        /// <summary>
        /// Information logging 
        /// </summary>
        Information,
        /// <summary>
        /// Log as warning
        /// </summary>
        Warning,
        /// <summary>
        /// Log as error
        /// </summary>
        Error,
        /// <summary>
        /// Details debug logging
        /// </summary>
        Debug
    }

	/// <summary>
	/// Used to log things.
	/// </summary>
    public class Logging
    {
		/// <summary>
		/// A triggering exception (if there was one).
		/// </summary>
		public Exception Exception;
        /// <summary>
        /// THe message to write to the log
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The level
        /// </summary>
        public LOG_LEVEL LogLevel { get; set; }
    }

    /// <summary>
    /// Events are instanced automatically. 
    /// You can however specify a custom type or instance them yourself if you'd like to do so.
    /// </summary>
    public partial class Events
	{
		/// <summary>
		/// All logging events.
		/// </summary>
		public static EventHandler<Logging> Logging;
	}
}