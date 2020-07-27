using System;


namespace Api.SocketServerLibrary
{
	
	/// <summary>
	/// Handles reading socket messages for a particular Client.
	/// </summary>
	public class ClientReader : SocketReader
	{
		/// <summary>
		/// The underlying client.
		/// </summary>
		public Client Source;

		private readonly Action<ushort> OnReadRequestId_D;
		private readonly Action<uint> OnReadUserId_D;
		/// <summary>
		/// Current message being received.
		/// </summary>
		private IMessage CurrentMessage;

		/// <summary>
		/// Creates a new client reader.
		/// </summary>
		/// <param name="source"></param>
		public ClientReader(Client source){
			Source = source;
			OnReadRequestId_D = new Action<ushort>(OnReadRequestId);
			OnReadUserId_D = new Action<uint>(OnReadUserId);
		}

		private void OnReadRequestId(ushort value)
		{
			CurrentMessage.RequestId = value;
			if (CurrentMessage.OpCode.RequiresUserId)
			{
				// Also read user ID:
				ReadUInt32(OnReadUserId_D);
			}
			else
			{
				// Run opcode now:
				CurrentMessage.OpCode.OnStartReceive(CurrentMessage);
			}
		}
		
		private void OnReadUserId(uint value)
		{
			CurrentMessage.UserId = value;
			CurrentMessage.OpCode.OnStartReceive(CurrentMessage);
		}

		/// <summary>
		/// Called to read the next opcode.
		/// </summary>
		public override void ReadOpcode(){
			// Opcodes are stored as packed ints:
			ReadCompressed(ThenReadOpcode);
		}

		private void ThenReadOpcode(ulong opcode) {
			// Get the target opcode:
			OpCode target;

			if (Source.Server.FastOpCodeMap != null)
			{
				// Read from opcode map:
				target = Source.Server.FastOpCodeMap[(int)opcode];
			}
			else
			{
				Source.Server.OpCodeMap.TryGetValue((uint)opcode, out target);
			}

			if(target == null || (Source.Hello && !target.IsHello))
			{
				// Invalid opcode.
				Console.WriteLine("Invalid opcode received: " + opcode + ". " + Source.Hello + ", " + target.IsHello);
				Source.Socket.Close();
				return;
			}

			// Create a message for this opcode:
			CurrentMessage = target.GetAMessageInstance();
			CurrentMessage.Client = Source;

			if (target.RequestId)
			{
				// Read context.RequestId first.
				ReadUInt16(OnReadRequestId_D);
			}
			else if (target.RequiresUserId)
			{
				// Read context.UserId. This is the actual user ID, not a network ID.
				ReadUInt32(OnReadUserId_D);
			}
			else
			{
				// Run msg receiving:
				target.OnStartReceive(CurrentMessage);
			}
		}
		
	}
}