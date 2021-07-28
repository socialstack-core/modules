namespace Api.SocketServerLibrary
{
	/// <summary>
	/// Handles reading a websocket header.
	/// </summary>
	public class WsHeaderReader : MessageReader
	{

		/// <summary>
		/// Globally shared reader.
		/// </summary>
		public static WsHeaderReader Instance = new WsHeaderReader();

		/// <summary>
		/// 
		/// </summary>
		public WsHeaderReader()
		{
			FirstDataRequired = 2;
		}

		/// <summary>
		/// Process is called when the reader has enough data available.
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="client"></param>
		public override void Process(ref RecvStackFrame frame, Client client)
		{
			while (client.BytesInBuffer >= frame.BytesRequired)
			{
				switch (frame.Phase)
				{
					case 0:
						var opcode = client.NextNoMask() & 15;

						if (opcode != 2)
						{
							client.Close();
							return;
						}

						var size = client.NextNoMask() & 127;

						if (size == 126)
						{
							// 2 byte length follows.
							frame.BytesRequired = 2;
							frame.Phase = 1;
						}
						else if (size == 127)
						{
							// 8 byte length follows.
							frame.BytesRequired = 8;
							frame.Phase = 2;
						}
						else
						{
							// Mask
							client.BytesUntilWebsocketHeader = size;
							frame.BytesRequired = 4;
							frame.Phase = 3;
						}
						
						break;
					case 1:
						// 2 byte size
						client.BytesUntilWebsocketHeader = client.NextNoMask() << 8 | client.NextNoMask();
						frame.BytesRequired = 4;
						frame.Phase = 3;
						break;
					case 2:
						// 8 byte size
						var len = (ulong)(
							client.NextNoMask() << 56 | client.NextNoMask() << 48 | client.NextNoMask() << 40 | client.NextNoMask() << 32 |
							client.NextNoMask() << 24 | client.NextNoMask() << 16 | client.NextNoMask() << 8 | client.NextNoMask()
						);

						client.BytesUntilWebsocketHeader = (int)len;
						frame.BytesRequired = 4;
						frame.Phase = 3;

						break;
					case 3:
						// Mask
						client.WebsocketMask[0] = client.NextNoMask();
						client.WebsocketMask[1] = client.NextNoMask();
						client.WebsocketMask[2] = client.NextNoMask();
						client.WebsocketMask[3] = client.NextNoMask();
						client.WebsocketMaskIndex = 0;

						if (client.BytesUntilWebsocketHeader == 0)
						{
							// This websocket packet was actually empty.
							// We're immediately expecting another one, so just reset the header reader.
							frame.Phase = 0;
							frame.BytesRequired = 2;
						}
						else
						{
							// Done reading the WS header.
							client.Pop();
						}

						return;

				}
			}
		}

	}
}