using Api.Contexts;
//using Api.WebSockets;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Api.SocketServerLibrary
{
	/// <summary>
	/// A get request.
	/// </summary>
	public class GetMessage : Message<GetMessage>
	{
		/// <summary>
		/// Content type.
		/// </summary>
		public int ContentType;
		/// <summary>
		/// ID of the content.
		/// </summary>
		public uint Id;
	}

	/// <summary>
	/// A content update.
	/// </summary>
	public class ContentUpdate : Message<ContentUpdate>
	{
		/// <summary>
		/// Number of bytes in the JSON.
		/// </summary>
		public int JsonLength;
		/// <summary>
		/// Mode.
		/// </summary>
		public byte Mode;
		/// <summary>
		/// Locale.
		/// </summary>
		public uint LocaleId;
		/// <summary>
		/// Raw json.
		/// </summary>
		public string Json;
	}

	/// <summary>
	/// A list request.
	/// </summary>
	public class ListMessage : Message<ListMessage>
	{
		/// <summary>
		/// Content type.
		/// </summary>
		public int ContentType;
		/// <summary>
		/// ID of the content.
		/// </summary>
		public uint Id;
	}

	/// <summary>
	/// A wrapped JSON request.
	/// </summary>
	public class JsonMessage : Message<JsonMessage>
	{
		/// <summary>
		/// The complete JSON string.
		/// </summary>
		public string Json;
	}

	/// <summary>
	/// Used when a websocket is connecting.
	/// </summary>
	public class WebsocketHandshake : OpCode
	{
		
		/// <summary>
		/// Instanced by calling aServer.AcceptWebsockets()
		/// </summary>
		/// <param name="requireApplicationHello"></param>
		public WebsocketHandshake(bool requireApplicationHello)
		{
			// Custom crafted msg reader:
			MessageReader = new WebSocketHandshakeReader(requireApplicationHello);
		}

		private ContextService _ctxService;

		/// <summary>
		/// Gets the context service.
		/// </summary>
		public ContextService ContextService
		{
			get {
				if (_ctxService == null)
				{
					_ctxService = Startup.Services.Get<ContextService>();
				}

				return _ctxService;
			}
		}

	}

	/// <summary>
	/// Reads websocket handshakes. Is a global shared instance as it has no connection specific state.
	/// </summary>
	public class WebSocketHandshakeReader : MessageReader
	{

		private bool RequireApplicationHello;

		/// <summary>
		/// 
		/// </summary>
		private byte[] UpperCaseKeyHeader;

		/// <summary>
		/// 
		/// </summary>
		private byte[] LowerCaseKeyHeader;

		/// <summary>
		/// 
		/// </summary>
		private byte[] UpperCaseCookieHeader;

		/// <summary>
		/// 
		/// </summary>
		private byte[] LowerCaseCookieHeader;

		/// <summary>
		/// 
		/// </summary>
		private byte[] MagicString;

		/// <summary>
		/// 
		/// </summary>
		public byte[] HeaderTerminal;

		/// <summary>
		/// 
		/// </summary>
		private byte[] ProtocolSwitchResponse;
		
		/// <summary>
		/// 
		/// </summary>
		public byte[] UpperCaseUserStart;

		/// <summary>
		/// 
		/// </summary>
		private byte[] LowerCaseUserStart;

		/// <summary>
		/// 
		/// </summary>
		private SHA1 Sha1;

		/// <summary>
		/// Context service
		/// </summary>
		private ContextService ContextService;

		/// <summary>
		/// The WS handshake reader
		/// </summary>
		public WebSocketHandshakeReader(bool requireApplicationHello)
		{
			RequireApplicationHello = requireApplicationHello;

			// Bytes required for these frames is always just 1.
			FirstDataRequired = 1;

			Sha1 = SHA1.Create();
			UpperCaseKeyHeader = System.Text.Encoding.ASCII.GetBytes("sec-websocket-key:");
			LowerCaseKeyHeader = System.Text.Encoding.ASCII.GetBytes("SEC-WEBSOCKET-KEY:");
			UpperCaseCookieHeader = System.Text.Encoding.ASCII.GetBytes("cookie:");
			LowerCaseCookieHeader = System.Text.Encoding.ASCII.GetBytes("COOKIE:");
			MagicString = System.Text.Encoding.ASCII.GetBytes("258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
			HeaderTerminal = System.Text.Encoding.ASCII.GetBytes("\r\n\r\n");
			ProtocolSwitchResponse = System.Text.Encoding.ASCII.GetBytes("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: ");
			UpperCaseUserStart = System.Text.Encoding.ASCII.GetBytes("USER=");
			LowerCaseUserStart = System.Text.Encoding.ASCII.GetBytes("user=");
		}

		/// <summary>
		/// Counts whitespace at the start of the given buffer.
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		public int WhitespaceOffset(BufferedBytes buffer)
		{
			var whitespaceOffset = 0;

			for (var i = 0; i < 5; i++)
			{
				if (buffer.Bytes[i] == ' ')
				{
					whitespaceOffset++;
				}
				else
				{
					break;
				}
			}

			return whitespaceOffset;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="client"></param>
		public override void Process(ref RecvStackFrame frame, Client client)
		{
			byte current;

			// While we have more bytes..
			while (client.BytesAvailable > 0)
			{
				if (frame.Phase == 0)
				{
					// Skip bytes until a \n.
					while (client.BytesAvailable > 0)
					{
						current = client.Next();

						if (current == '\n')
						{
							frame.Phase = 1;
							break;
						}
					}
				}
				else if (frame.Phase == 1)
				{
					current = client.Next();
					
					if (current == '\r')
					{
						// End of header! We're all done here.
						frame.Phase = 4;
					}
					else if (current == UpperCaseKeyHeader[0] || current == LowerCaseKeyHeader[0])
					{
						// Possibly reading a key header.
						frame.Phase = 1 << 8 | 1;
					}
					else if (current == UpperCaseCookieHeader[0] || current == LowerCaseCookieHeader[0])
					{
						// Possibly reading a cookie header.
						frame.Phase = 2 << 8 | 1;
					}
					else
					{
						// We don't care about this header - skip it.
						frame.Phase = 0;
					}
				}
				else if (frame.Phase == 2)
				{
					// Read sec value
					
					while (client.BytesAvailable > 0)
					{
						current = client.Next();

						if (current == '\r')
						{
							// Finished reading this header line.

							// Next, write the magic string into the buffer too (it's more than big enough to support this).
							for (var i = 0; i < MagicString.Length; i++)
							{
								client.ScratchSpace.Bytes[client.ScratchSpace.Offset++] = MagicString[i];
							}

							// Count whitespace at the start:
							var whitespace = WhitespaceOffset(client.ScratchSpace);

							// Convert to b64:
							string b64 = Convert.ToBase64String(Sha1.ComputeHash(client.ScratchSpace.Bytes, whitespace, client.ScratchSpace.Offset - whitespace));

							// Release the scratch buffer:
							client.ScratchSpace.Release();
							client.ScratchSpace = null;

							// Send protocol switch.
							var writer = Writer.GetPooled();
							writer.Start(ProtocolSwitchResponse);

							// Write each byte of the string:
							for (var i = 0; i < b64.Length; i++)
							{
								writer.Write((byte)b64[i]);
							}

							// Write \r\n\r\n:
							for (var i = 0; i < 4; i++)
							{
								writer.Write(HeaderTerminal[i]);
							}

							client.Send(writer);
							writer.Release();

							frame.Phase = 0;
							break;
						}
						else
						{
							// This byte is part of our header line.
							client.ScratchSpace.Bytes[client.ScratchSpace.Offset++] = current;
						}
					}

				}
				else if (frame.Phase == 3)
				{
					// Read cookie value

					while (client.BytesAvailable > 0)
					{
						current = client.Next();

						if (current == ';' || current == '\r')
						{
							// Completed reading a cookie - try processing it and clear the scratch space.
							var nonUser = false;
							var whitespace = WhitespaceOffset(client.ScratchSpace);

							if ((client.ScratchSpace.Offset - whitespace) >= 5)
							{
								for (var i = 0; i < 5; i++)
								{
									var currentChar = client.ScratchSpace.Bytes[whitespace + i];

									if (currentChar != UpperCaseUserStart[i] && currentChar != LowerCaseUserStart[i])
									{
										nonUser = true;
										break;
									}
								}
							}

							if (nonUser)
							{
								// Not a user cookie. Just ignore this data.
								client.ScratchSpace.Offset = 0;
							}
							else
							{
								// User cookie!
								// Its value starts at whitespace + 5.
								var length = client.ScratchSpace.Offset - (whitespace + 5);
								
								var userCookie = System.Text.Encoding.UTF8.GetString(client.ScratchSpace.Bytes, whitespace + 5, length);

								// Gets the context and applies it to the given client.
								// MUST wait for this before processing anything else from the socket.
								var task = Task.Run(async () =>
								{
									// Apply context:
									if (ContextService == null)
									{
										ContextService = Startup.Services.Get<ContextService>();
									}

									var context = await ContextService.Get(userCookie);

									if (context == null)
									{
										// Use an anon one instead:
										context = new Context();
									}

									await client.SetContext(context);

									// Allow the client to continue:
									client.TaskCompletedContinueReceive();
								});

								if (!task.IsCompleted)
								{
									// Release the scratch buffer and halt.
									client.ScratchSpace.Release();
									client.ScratchSpace = null;
									frame.Phase = 0;

									// It hasn't completed inline and will start soon - mark beforeReceive as true.
									client.WaitForTaskBeforeReceive = task;

									return;
								}

							}

							if (current == '\r')
							{
								// Release the scratch buffer:
								client.ScratchSpace.Release();
								client.ScratchSpace = null;

								frame.Phase = 0;
								break;
							}

						}
						else
						{
							// This byte is part of our cookie header line.
							// If we're about to blow the limit, this cookie is too long to be a user cookie so just treat it as junk.
							if (client.ScratchSpace.Offset == BinaryBufferPool.OneKb.BufferSize)
							{
								// Little bit of space for the cookie name:
								client.ScratchSpace.Offset = 40;
							}
							else
							{
								client.ScratchSpace.Bytes[client.ScratchSpace.Offset++] = current;
							}
						}
					}

				}
				else if (frame.Phase == 4)
				{
					// Exiting. Read the \n:
					client.Next();

					// Pop this frame:
					client.Pop();


					// New clients always need to allocate anyway, so it's better to allocate this now
					// (particularly as it will last throughout the duration of the websocket link anyway).
					client.WebsocketMask = new byte[4];

					// Immediately expecting a websocket header - Push a header reader onto the stack.
					client.BytesUntilWebsocketHeader = 0;

					client.RecvStackPointer++;
					client.RecvStack[client.RecvStackPointer] = new RecvStackFrame()
					{
						Reader = WsHeaderReader.Instance,
						Phase = 0,
						BytesRequired = 2
					};

					// Successful handshake. Clear hello flag if the application wishes to do so:
					if (!RequireApplicationHello)
					{
						client.Hello = false;
					}

					return;
				}
				else
				{
					var phase = frame.Phase >> 8; // 1 or 2.
					var index = frame.Phase & 255; // 0->255

					// Listening out for cookie and sec header values.
					current = client.Next();

					if (phase == 1)
					{
						// sec header
						if (current == UpperCaseKeyHeader[index] || current == LowerCaseKeyHeader[index])
						{
							// Test the next one.
							if (index == LowerCaseKeyHeader.Length - 1)
							{
								// Sec header is indeed incoming!
								client.ScratchSpace = BinaryBufferPool.OneKb.Get();
								frame.Phase = 2;
							}
							else
							{
								frame.Phase++;
							}
						}
						else
						{
							// Not a header we care about.
							frame.Phase = 0;
						}
					}
					else
					{
						// Cookie header
						if (current == UpperCaseCookieHeader[index] || current == LowerCaseCookieHeader[index])
						{
							// Test the next one.
							if (index == LowerCaseCookieHeader.Length - 1)
							{
								// Cookie header is indeed incoming!
								client.ScratchSpace = BinaryBufferPool.OneKb.Get();
								frame.Phase = 3;
							}
							else
							{
								frame.Phase++;
							}
						}
						else
						{
							// Not a header we care about.
							frame.Phase = 0;
						}
					}

				}

			}
		}

	}
}