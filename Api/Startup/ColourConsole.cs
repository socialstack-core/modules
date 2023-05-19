using System;
using System.Text.RegularExpressions;

namespace Api.ColourConsole
{
    /// <summary>
    /// </summary>
	public static class WriteColourLine
    {
		/// <summary>
		/// Outputs [marked] keywords in green (or the entire string if nothing marked)
		/// </summary>
		/// <param name="message"></param>
		[Obsolete("Use Log.Ok instead")]
		public static void Success(string message)
        {
            Log.Ok("", null, message);
        }

		/// <summary>
		/// Outputs [marked] keywords in red (or the entire string if nothing marked)
		/// </summary>
		/// <param name="message"></param>
		[Obsolete("Use Log.Error instead")]
		public static void Error(string message)
        {
            Log.Error("", null, message);
        }

		/// <summary>
		/// Outputs [marked] keywords in yellow (or the entire string if nothing marked)
		/// </summary>
		/// <param name="message"></param>
		[Obsolete("Use Log.Warn instead")]
		public static void Warning(string message)
        {
            Log.Warn("", null, message);
        }

		/// <summary>
		/// Outputs [marked] keywords in cyan (or the entire string if nothing marked)
		/// </summary>
		/// <param name="message"></param>
		[Obsolete("Use Log.Info instead")]
		public static void Info(string message)
        {
            Log.Info("", null, message);
        }
    }
    
}
