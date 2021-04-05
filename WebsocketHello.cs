using Api.Contexts;
using System;
using System.Security.Cryptography;


namespace Api.SocketServerLibrary
{
	public class GetMessage : Message
	{
		public int ContentType;
		public int Id;
	}

	/// <summary>
	/// Used when a websocket is connecting.
	/// </summary>
	public class WebsocketHandshake : OpCode
	{
		public byte[] UpperCaseKeyHeader;
		public byte[] LowerCaseKeyHeader;
		public byte[] HeaderTerminal;
		public byte[] MagicString;
		public byte[] ProtocolSwitchResponse;
		public SHA1Managed Sha1;


		/// <summary>
		/// Instanced by calling aServer.AcceptWebsockets()
		/// </summary>
		public WebsocketHandshake()
		{
			Sha1 = new SHA1Managed();

			UpperCaseKeyHeader = System.Text.Encoding.ASCII.GetBytes("sec-websocket-key:");
			LowerCaseKeyHeader = System.Text.Encoding.ASCII.GetBytes("SEC-WEBSOCKET-KEY:");
			MagicString = System.Text.Encoding.ASCII.GetBytes("258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
			HeaderTerminal = System.Text.Encoding.ASCII.GetBytes("\r\n\r\n");
			ProtocolSwitchResponse = System.Text.Encoding.ASCII.GetBytes("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: ");
		}

		/// <summary>
		/// Gets a message instance to use for this opcode.
		/// </summary>
		/// <returns></returns>
		public override IMessage GetAMessageInstance()
		{
			IMessage msg;

			lock (PoolLock)
			{
				if (First == null)
				{
					msg = new WebsocketHandshakeMessage();
				}
				else
				{
					msg = First;
					First = msg.After;
					msg.Pooled = false;
				}
			}

			msg.OpCode = this;
			return msg;
		}

		/// <summary>
		/// Runs when the given message has started to be received.
		/// </summary>
		/// <param name="message"></param>
		public override void OnStartReceive(IMessage message)
		{
			// We have full control of the current client.
			// This must read the websocket header, then call OnReceive.

			// So far we have read a 'G' - the following bytes include a URL which we don't use and can be anything.
			// So instead, we just keep skipping bytes until we encounter \n.
			(message as WebsocketHandshakeMessage).Handle();
		}

	}

	/// <summary>
	/// Stores context during a websocket handshake. Like all other primary message types, these objects are pooled.
	/// </summary>
	public class WebsocketHandshakeMessage : Message
	{
		public WebsocketHandshakeMessage()
		{
			SkipUntilNewline_D = new Action<byte>(SkipUntilNewline);
			OnCheckSecHeader_D = new Action<byte>(OnCheckSecHeader);
			CheckEndOfHeader_D = new Action<byte>(CheckEndOfHeader);
			ReadSecHeaderValue_D = new Action<byte>(ReadSecHeaderValue);
		}

		/// <summary>
		/// Start receiving this msg.
		/// </summary>
		public void Handle()
		{
			var reader = Client.Reader;

			// Clear str_1 - the fact that it's set later indicates a sec header was sent.
			String_1 = null;

			// Read the first line is "GET /url HTTP/1.1\r\n". When we're done reading it, we need to find the Sec-WebSocket-Key header
			reader.Read(SkipUntilNewline_D);
		}

		private int Int32_1;
		private string String_1;
		private readonly Action<byte> SkipUntilNewline_D;
		private readonly Action<byte> OnCheckSecHeader_D;
		private readonly Action<byte> CheckEndOfHeader_D;
		private readonly Action<byte> ReadSecHeaderValue_D;

		private void SkipUntilNewline(byte lastReadByte)
		{
			if (lastReadByte != '\n')
			{
				// Go again
				Client.Reader.Read(SkipUntilNewline_D);
				return;
			}

			// Find the Sec-WebSocket-Key header.

			// We'll do this by keeping track of which character we're up to in the message and read a byte at a time.
			// If we encounter a character other than what we're expecting next, we just skip until a newline and try again.
			Int32_1 = 0;
			Client.Reader.Read(OnCheckSecHeader_D);
		}

		private void OnCheckSecHeader(byte character)
		{
			var opcode = OpCode as WebsocketHandshake;

			// Character MUST be either..
			if (character == opcode.UpperCaseKeyHeader[Int32_1] || character == opcode.LowerCaseKeyHeader[Int32_1])
			{

				// Great - get the next character:
				Int32_1++;

				if (Int32_1 >= opcode.UpperCaseKeyHeader.Length)
				{
					// That's the lot! Next we have the value.
					// Grab a buffer from the pool where we'll store the field value.
					Client.Reader.Push(BinaryBufferPool.Get());
					Int32_1 = 0;

					Client.Reader.Read(ReadSecHeaderValue_D);

				}
				else
				{
					Client.Reader.Read(OnCheckSecHeader_D);
				}

				return;
			}

			// Not the header we're after. Skip until we get a \n:
			Client.Reader.Read(SkipUntilNewline_D);
		}

		/// <summary>
		/// Reading for the value of Sec-WebSocket-Key
		/// </summary>
		private void ReadSecHeaderValue(byte character)
		{
			var binaryBuffer = (BufferedBytes)Client.Reader.Peek();

			if (character != '\r')
			{
				binaryBuffer.Bytes[Int32_1++] = character;
				Client.Reader.Read(ReadSecHeaderValue_D);
				return;
			}

			// That's the end of the field. Write the magic string into the buffer too (it's more than big enough to support this).
			Client.Reader.Pop();

			var opcode = OpCode as WebsocketHandshake;

			for (var i = 0; i < opcode.MagicString.Length; i++)
			{
				binaryBuffer.Bytes[Int32_1++] = opcode.MagicString[i];
			}

			// First establish how much whitespace is at the start (it'll be either 0 or 1, but the spec permits no particular limit so we permit 5).
			var whitespaceCount = 0;

			for (var i = 0; i < 5; i++)
			{
				if (binaryBuffer.Bytes[i] == ' ')
				{
					whitespaceCount = i + 1;
				}
				else
				{
					break;
				}
			}

			// Need to now create the SHA1 of binaryBuffer.Bytes[whitespaceCount] to binaryBuffer.Bytes[context.Int32_1-1], then the base64 of that.
			String_1 = Convert.ToBase64String(opcode.Sha1.ComputeHash(binaryBuffer.Bytes, whitespaceCount, Int32_1 - whitespaceCount));

			// Release the buffer:
			binaryBuffer.Release();

			// Now must burn bytes until we see \r\n\r\n at the end of the header.
			Int32_1 = 0;

			Client.Reader.Read(CheckEndOfHeader_D);
		}

		/// <summary>
		/// Watches out for \r\n\r\n at the end of the header.
		/// </summary>
		private void CheckEndOfHeader(byte currentByte)
		{
			var opcode = OpCode as WebsocketHandshake;

			if (currentByte == opcode.HeaderTerminal[Int32_1])
			{
				// Read the next byte of the terminal - expect the next one:
				Int32_1++;

				if (Int32_1 == 4)
				{
					// That's the end of the header - we've received the whole thing!
					// Now send back our websocket handshake response.
					SendWebsocketHandshakeResponse();
					return;
				}
			}
			else
			{
				// Nope - not the terminal yet; reset to 0.
				Int32_1 = 0;
			}

			Client.Reader.Read(CheckEndOfHeader_D);
		}

		/// <summary>
		/// Sends the websocket handshake response.
		/// </summary>
		private void SendWebsocketHandshakeResponse()
		{

			var opcode = OpCode as WebsocketHandshake;

			if (String_1 == null)
			{
				// Didn't send the ws key header. Kill the request.
				opcode.Kill(this);
				return;
			}
			
			// Most of this response header is just totally static data.
			var writer = Writer.GetPooled();
			writer.Start(opcode.ProtocolSwitchResponse);

			// Write each byte of the string:
			for (var i = 0; i < String_1.Length; i++)
			{
				writer.Write((byte)String_1[i]);
			}

			// Write \r\n\r\n:
			for (var i = 0; i < 4; i++)
			{
				writer.Write(opcode.HeaderTerminal[i]);
			}

			Client.Send(writer);

			// New clients always need to allocate anyway, so it's better to allocate this now
			// (particularly as it will last throughout the duration of the websocket link anyway).
			Client.Reader.WebsocketMask = new byte[4];

			// Immediately expecting a websocket header:
			Client.Reader.BytesUntilWebsocketHeader = 0;

			opcode.Done(this, true);

			// Note that although we got a successful handshake, we don't clear the hello flag.
			// That's because websocket just wraps other messages - our actual protocol hasn't done its hello yet.
		}


	}
}