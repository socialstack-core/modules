using Api.SocketServerLibrary;
using Api.WebSockets;
using System;
using System.Runtime.InteropServices;

namespace Api.ContentSync{
	
	/// <summary>
	/// Used when reading remote type updates.
	/// </summary>
	public class SyncServerRemoteReader : MessageReader
	{
		/// <summary>
		/// 1 = Create, 2 = Update, 3 = Delete.
		/// </summary>
		public int Action;

		/// <summary>
		/// The opcode this reader is for.
		/// </summary>
		public OpCode<SyncServerRemoteType> OpCode;

		/// <summary>
		/// Content sync service.
		/// </summary>
		public WebSocketService WebsocketService;

		/// <summary>
		/// Used when reading remote type updates.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="wsService"></param>
		public SyncServerRemoteReader(int action, WebSocketService wsService)
		{
			Action = action;
			FirstDataRequired = 4;
			WebsocketService = wsService;
		}

		/// <summary>
		/// Processes this received frame.
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="client"></param>
		public override void Process(ref RecvStackFrame frame, Client client)
		{
			var target = (SyncServerRemoteType)frame.TargetObject;

			switch(frame.Phase)
			{
				case 0:
					// Message size
					// Read the size:
					frame.Phase = 1;
					frame.BytesRequired = 1;
					target.RemainingBytes = (uint)(client.Next()) | (uint)(client.Next() << 8) | (uint)(client.Next() << 16) | (uint)(client.Next() << 24);
					break;
				case 1:
					// Locale ID - switch byte.
					var localeId = client.Next();
					target.RemainingBytes--;

					if (localeId < 251)
					{
						target.LocaleId = localeId;
						frame.Phase = 5;
						frame.BytesRequired = 1;
						return;
					}
					else if (localeId == 251)
					{
						// 2 bytes needed
						frame.Phase = 2;
						frame.BytesRequired = 2;
					}
					else if (localeId == 252)
					{
						// 3 bytes needed
						frame.Phase = 3;
						frame.BytesRequired = 3;
					}
					else if (localeId == 253)
					{
						// 4 bytes needed
						frame.Phase = 4;
						frame.BytesRequired = 4;
					}
					// 8 byte opcodes aren't permitted.

					break;
				case 2:
					// Compressed (ushort)
					target.LocaleId = (uint)(client.Next() | (client.Next() << 8));
					target.RemainingBytes-=2;
					frame.Phase = 5;
					frame.BytesRequired = 1;
					return;
				case 3:
					// Compressed (3 byte)
					target.LocaleId = (uint)(client.Next() | (client.Next() << 8) | (client.Next() << 16));
					target.RemainingBytes -= 3;
					frame.Phase = 5;
					frame.BytesRequired = 1;
					return;
				case 4:
					// Compressed (4 byte)
					target.LocaleId = (uint)(client.Next() | (client.Next() << 8) | (client.Next() << 16) | (client.Next() << 24));
					target.RemainingBytes -= 4;
					frame.Phase = 5;
					frame.BytesRequired = 1;
					return;
				case 5:
					// Single byte type name length, null offset. The -1 brings it down to the correct size.
					var nameSize = client.Next() - 1;
					target.RemainingBytes -= 1;
					frame.Phase = 6;
					frame.BytesRequired = nameSize;

					break;
				case 6:
					// Textual type name.
					var size = frame.BytesRequired;
					target.RemainingBytes -= (uint)size;
					frame.BytesRequired = (int)target.RemainingBytes;

					frame.Phase = 7;

					// Allocation required for this one:
					var buff = new byte[size];

					for (var i = 0; i < size; i++)
					{
						buff[i] = client.Next();
					}

					// Get the type name:
					var typeName = System.Text.Encoding.UTF8.GetString(buff);

					// Get the meta:
					var meta = WebsocketService.GetMeta(typeName);

					target.Meta = meta;

					if (meta == null)
					{
						// Skip:
						frame.Phase = 8;
					}
					else
					{
						frame.Phase = 7;
					}

					break;
				case 7:
					// Got all the bytes in the buffer - read it now.

					// Instance an object and read into it.
					target.Meta.Handle(OpCode, client, target, Action);

					// Done:
					client.Pop();
				break;
				case 8:
					//  Got all the bytes in the buffer - skip it now.
					client.Skip(frame.BytesRequired);
					client.Pop();
				break;
			}
		}

	}
	
}