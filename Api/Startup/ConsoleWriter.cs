using System;
using System.IO;
using System.Text;

namespace Api.Startup
{
	
	/// <summary>
	/// Handles cloned console data such that it can be accessed more easily by admins.
	/// </summary>
	public class ConsoleWriter: TextWriter
	{
		/// <summary>
		/// Underlying stream.
		/// </summary>
		private TextWriter _main;
		
		/// <summary>
		/// Writes to the underlying text stream.
		/// </summary>
		/// <param name="message"></param>
		public void WriteBase(string message)
		{
			_main.Write(message);
		}

		/// <summary>
		/// Creates a writer for the given main output stream.
		/// </summary>
		public ConsoleWriter(TextWriter mainStream)
		{
			_main = mainStream;
		}
		
		/// <summary>
		/// Writes 1 char.
		/// </summary>
		public override void Write(char value)
		{
			_main.Write(value);

			// And also add to the logging system:
			Log.FromStdOut(value+"");
		}
		
		/// <summary>
		/// Writes a string.
		/// </summary>
		public override void Write(string value)
		{
			_main.Write(value);

			// And also add to the logging system:
			Log.FromStdOut(value);
		}
		
		/// <summary>
		/// Encoding
		/// </summary>
		public override Encoding Encoding
		{
			get { return Encoding.Default; }
		}
	}
}