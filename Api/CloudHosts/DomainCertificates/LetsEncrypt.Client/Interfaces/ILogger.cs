using System;
using System.Threading.Tasks;

namespace LetsEncrypt.Client.Interfaces
{
    /// <summary>
    /// </summary>
    public interface ILogger
    {
        // Error

        /// <summary>
        /// </summary>
        void LogError(Exception ex);

        /// <summary>
        /// </summary>
        Task LogErrorAsync(Exception ex);

        /// <summary>
        /// </summary>
        void LogError(string subject, string message = null);

        /// <summary>
        /// </summary>
        Task LogErrorAsync(string subject, string message = null);

        // Message

        /// <summary>
        /// </summary>
        void LogMessage(string subject, string message = null);

        /// <summary>
        /// </summary>
        Task LogMessageAsync(string subject, string message = null);
    }
}