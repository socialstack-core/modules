using System;


namespace Api.SocketServerLibrary
{
	
	/// <summary>
	/// Reads generic messages which start with [opcode][4 byte int size][payload]
	/// </summary>
	public class GenericMessageReader<T> : MessageReader
		 where T : Message<T>, new()
	{
		private BoltReaderWriter<T> BoltIO;
		private OpCode<T> OpCode;

		/// <summary>
		/// Creates a message reader for the given bolt IO.
		/// </summary>
		/// <param name="boltIO"></param>
		/// <param name="opcode"></param>
		public GenericMessageReader(BoltReaderWriter<T> boltIO, OpCode<T> opcode)
		{
			FirstDataRequired = 4;
			BoltIO = boltIO;
			OpCode = opcode;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="client"></param>
		public override void Process(ref RecvStackFrame frame, Client client)
		{
			if (frame.Phase == 0)
			{
				// Read the size:
				frame.Phase = 1;
				frame.BytesRequired = client.Next() | client.Next() << 8 | client.Next() << 16 | client.Next() << 24;
			}
			else if(frame.Phase == 1)
			{
				try
				{
					// Read:
					var target = (T)frame.TargetObject;
					BoltIO.Read(target, client);

					// Receive:
					OpCode.OnReceive(client, target);
				}
				catch (Exception)
				{
                    Log.Warn("socketserverlibrary", "invalid client message received. Ignoring it.");
					client.Close();
					return;
				}

				// Done:
				client.Pop();
			}
		}
	}
	
}