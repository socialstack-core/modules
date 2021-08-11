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
		private const int maxLength = 4096;
		
		/// <summary>
		/// Underlying stream.
		/// </summary>
		private TextWriter _main;
		
		/// <summary>
		/// The backbuffer which tracks the last 4k characters that were passed through. It's a ring buffer.
		/// </summary>
		private char[] _buffer = new char[maxLength];
		
		private int _head;
		
		private int _size;
		
		/// <summary>
		/// Gets the latest block of text written to the writer.
		/// </summary>
		public string GetLatest()
		{
			return string.Create(_size, this, (Span<char> target, ConsoleWriter cw) => {
				for (var i = 0; i < cw._size; i++)
				{
					var index = cw._head - cw._size + i;

					if (index < 0)
					{
						index += maxLength;
					}

					target[i] = cw._buffer[index];
				}
			});
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
			_buffer[_head] = value;
			_head++;
			if (_head == maxLength)
			{
				_head = 0;
			}
			if (_size < maxLength)
			{
				_size++;
			}
		}
		
		/// <summary>
		/// Writes a string.
		/// </summary>
		public override void Write(string value)
		{
			_main.Write(value);

			if (value == null)
			{
				return;
			}
			
			for(var i =0;i<value.Length;i++)
			{
				var c = value[i];
				_buffer[_head] = c;
				_head++;
				if (_head == maxLength)
				{
					_head = 0;
				}
				if (_size < maxLength)
				{
					_size++;
				}
			}
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