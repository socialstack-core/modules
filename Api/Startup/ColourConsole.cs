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
        public static void Success(string message)
        {
            OutputMessage(message, ConsoleColor.Green);
        }

        /// <summary>
        /// Outputs [marked] keywords in red (or the entire string if nothing marked)
        /// </summary>
        /// <param name="message"></param>
        public static void Error(string message)
        {
            OutputMessage(message, ConsoleColor.Red);
        }

        /// <summary>
        /// Outputs [marked] keywords in yellow (or the entire string if nothing marked)
        /// </summary>
        /// <param name="message"></param>
        public static void Warning(string message)
        {
            OutputMessage(message, ConsoleColor.Yellow);
        }

        /// <summary>
        /// Outputs [marked] keywords in cyan (or the entire string if nothing marked)
        /// </summary>
        /// <param name="message"></param>
        public static void Info(string message)
        {
            OutputMessage(message, ConsoleColor.Cyan);
        }

        private static void OutputMessage(string message, ConsoleColor color)
        {
            var pieces = Regex.Split(message, @"(\[[^\]]*\])");

            if (pieces.Length == 1)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
                return;
            }

            for (int i = 0; i < pieces.Length; i++)
            {
                string piece = pieces[i];

                if (piece.StartsWith("[") && piece.EndsWith("]"))
                {
                    Console.ForegroundColor = color;

                    // only hide the surrounding square brackets if it's not at the start of the message;
                    // e.g. "[WARN] message content" will include them, "this is a [warning]" will not
                    if (!(i == 1 && pieces[0] == ""))
                    {
                        piece = piece.Substring(1, piece.Length - 2);
                    }

                }

                Console.Write(piece);
                Console.ResetColor();
            }

            Console.WriteLine();
        }
    }
    
}
