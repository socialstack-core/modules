using System;
using System.Runtime.InteropServices;


namespace Api.SocketServerLibrary{
	
	/// <summary>
	/// Used when reading a complete message. It is outputted as a writer, opcode included.
	/// </summary>
	public class CompleteMessageReader : MessageReader
	{
		private readonly CompleteMessageOpCode OpCode;

		/// <summary>
		/// Used when reading a complete message, typically for forwarding to other servers.
		/// </summary>
		public CompleteMessageReader(CompleteMessageOpCode opcode)
		{
			FirstDataRequired = 4;
			OpCode = opcode;
		}

		/// <summary>
		/// Processes this received frame.
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="client"></param>
		public override void Process(ref RecvStackFrame frame, Client client)
		{
			Writer writer = (Writer)frame.TargetObject;

			switch (frame.Phase)
			{
				case 0:
					// Message size

					// Read the size:
					frame.Phase = 1;
					frame.TargetObject = writer;
					var messageSize = (uint)(client.Next()) | (uint)(client.Next() << 8) | (uint)(client.Next() << 16) | (uint)(client.Next() << 24);
					frame.BytesRequired = (int)messageSize;
					writer.Write(messageSize);

					if (messageSize == 0)
					{
						// Got all the bytes in the client buffer. Read them all now.
						client.BlockTransfer(frame.BytesRequired, writer);

						// Run the callback:
						OpCode.OnRequest(client, writer);

						// Done:
						client.Pop();
					}

					break;
				case 1:
					// Got all the bytes in the client buffer. Read them all now.
					client.BlockTransfer(frame.BytesRequired, writer);

					// Run the callback:
					OpCode.OnRequest(client, writer);

					// Done:
					client.Pop();
				break;
			}
		}

	}
	
}