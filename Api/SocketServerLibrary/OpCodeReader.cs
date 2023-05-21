using System;

namespace Api.SocketServerLibrary
{
	/// <summary>
	/// Handles reading an opcode.
	/// </summary>
	public class OpCodeReader : MessageReader {

		/// <summary>
		/// Globally shared opcode reader.
		/// </summary>
		public static OpCodeReader Instance = new OpCodeReader();

		/// <summary>
		/// 
		/// </summary>
		public OpCodeReader()
		{
			FirstDataRequired = 1;
		}

		private void HandleOpCode(uint opcode, Client client)
		{
			OpCode target;

			if (client.Server.FastOpCodeMap != null && opcode < client.Server.FastOpCodeMap.Length)
			{
				// Read from opcode map:
				target = client.Server.FastOpCodeMap[(int)opcode];
			}
			else
			{
				client.Server.OpCodeMap.TryGetValue(opcode, out target);
			}

			if (target == null || (client.Hello && !target.IsHello))
			{
				// Invalid opcode.
				Log.Info("socketserverlibrary", "Invalid opcode received: " + opcode + ". " + client.Hello + ", " + (target == null ? "[Not found]" : target.IsHello));
				client.Close();
				return;
			}

			target.Start(client);
		}

		/// <summary>
		/// Process is called when the reader has enough data available.
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="client"></param>
		public override void Process(ref RecvStackFrame frame, Client client)
		{
			uint opcode;

			while (client.BytesAvailable >= frame.BytesRequired)
			{
				switch (frame.Phase)
				{
					case 0:
						// First byte
						opcode = client.Next();

						if (opcode < 251)
						{
							// Opcode known (its value is just firstByte).
							HandleOpCode(opcode, client);
							return;
						}
						else if (opcode == 251)
						{
							// 2 bytes needed
							frame.Phase = 1;
							frame.BytesRequired = 2;
						}
						else if (opcode == 252)
						{
							// 3 bytes needed
							frame.Phase = 2;
							frame.BytesRequired = 3;
						}
						else if (opcode == 253)
						{
							// 4 bytes needed
							frame.Phase = 3;
							frame.BytesRequired = 4;
						}
						// 8 byte opcodes aren't permitted.

						break;
					case 1:
						// Compressed (ushort)
						opcode = (uint)(client.Next() | (client.Next() << 8));
						HandleOpCode(opcode, client);
						return;
					case 2:
						// Compressed (3 byte)
						opcode = (uint)(client.Next() | (client.Next() << 8) | (client.Next() << 16));
						HandleOpCode(opcode, client);
						return;
					case 3:
						// Compressed (4 byte)
						opcode = (uint)(client.Next() | (client.Next() << 8) | (client.Next() << 16) | (client.Next() << 24));
						HandleOpCode(opcode, client);
						return;
						// 8 byte opcodes aren't permitted.
				}
			}
		}

	}
}