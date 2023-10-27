using Api.SocketServerLibrary;


namespace Api.WebSockets{
	
	/// <summary>
	/// Reads wrapped JSON messages. These almost exclusively come from the frontend.
	/// </summary>
	public class JsonMessageReader  : MessageReader
	{
		/// <summary>
		/// The opcode this reader is for.
		/// </summary>
		public OpCode<JsonMessage> OpCode;
		
		/// <summary>
		/// Used when reading wrapped JSON messages.
		/// </summary>
		public JsonMessageReader()
		{
			FirstDataRequired = 1;
		}

		/// <summary>
		/// Processes this received frame.
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="client"></param>
		public override void Process(ref RecvStackFrame frame, Client client)
		{
			switch(frame.Phase)
			{
				case 0:
					// Size - switch byte.
					var length = client.Next();

					if (length < 251)
					{
						frame.Phase = 4;
						frame.BytesRequired = length - 1; // -1 as it is null offset.
						return;
					}
					else if (length == 251)
					{
						// 2 bytes needed
						frame.Phase = 1;
						frame.BytesRequired = 2;
					}
					else if (length == 252)
					{
						// 3 bytes needed
						frame.Phase = 2;
						frame.BytesRequired = 3;
					}
					else if (length == 253)
					{
						// 4 bytes needed
						frame.Phase = 3;
						frame.BytesRequired = 4;
					}
					// 8 byte length JSON isn't permitted.

					break;
				case 1:
					// Compressed (ushort)
					frame.Phase = 4;
					frame.BytesRequired = (int)(client.Next() | (client.Next() << 8)) - 1;
					return;
				case 2:
					// Compressed (3 byte)
					frame.Phase = 4;
					frame.BytesRequired = (int)(client.Next() | (client.Next() << 8) | (client.Next() << 16)) - 1;
					return;
				case 3:
					// Compressed (4 byte)
					frame.Phase = 4;
					frame.BytesRequired = (int)(client.Next() | (client.Next() << 8) | (client.Next() << 16) | (client.Next() << 24)) - 1;
					return;
				case 4:
					// The JSON.
					var size = frame.BytesRequired;

					// Allocation required for this one:
					var buff = new byte[size];

					for (var i = 0; i < size; i++)
					{
						buff[i] = client.Next();
					}

					// Get the JSON:
					var json = System.Text.Encoding.UTF8.GetString(buff);

					var target = (JsonMessage)frame.TargetObject;
					target.Json = json;

					// Trigger the opcode:
					OpCode.OnReceive(client, target);

					// Done:
					client.Pop();
				break;
			}
		}

	}
	
}