using System;
using System.Text;

namespace Api.SocketServerLibrary
{

	/// <summary>
	/// Used in debug to ensure the data streams remain in sync.
	/// </summary> 
	public class ProbeMeta
	{
		/// <summary>
		/// All the types of things that were written to the message.
		/// </summary>
		public ProbeMetaEntry[] Messages = new ProbeMetaEntry[500];

		/// <summary>
		/// Create a new probe meta.
		/// </summary>
		public ProbeMeta()
		{
			// The first two are always opcode and compressed.
			Messages[0].Type = MetaFieldType.Opcode;
			Messages[0].Length = 1;
			Messages[1].Type = MetaFieldType.Compressed;
			Messages[1].Length = 1;
		}
		
		/// <summary>
		/// The opcode of the message being documented.
		/// </summary>
		public ulong OpCode;
		
		/// <summary>
		/// Where we've verified to (on read).
		/// </summary>
		public int VerifiedTo;

		/// <summary>
		/// True if we're currently reading a probe message (and all verifies should be ignored).
		/// </summary>
		public bool LoadingProbeMessage;

		/// <summary>
		/// Where the array is filled to.
		/// </summary>
		public int FilledTo;

		
		/// <summary>Outputs to the log.</summary>
		public void DebugThisNow(int failedAtIndex, string failLineText)
		{
			var sb = new StringBuilder();
			sb.Append("== OpCode " + OpCode + " ==\r\n");
			
			for(var i=1;i<FilledTo;i++){
				
				var msgType = Messages[i].ToString();
				
				if(failedAtIndex == i){
					sb.Append(msgType + " <----- this was sent, but we tried to read a " + failLineText);
				}else if(i > failedAtIndex){
					sb.Append(msgType + " sent");
				}else{
					sb.Append(msgType);
				}

				sb.Append("\r\n");
				
			}

			Log.Info("socketserverlibrary", sb.ToString());
		}
		
		/// <summary>
		/// Resets the probe meta.
		/// </summary>
		public void Reset()
		{
			VerifiedTo = 0;
			FilledTo = 2;
		}
	}

	/// <summary>
	/// A particular entry in the probe meta.
	/// </summary>
	public struct ProbeMetaEntry {

		/// <summary>
		/// A non-entry.
		/// </summary>
		public static ProbeMetaEntry Nothing = new ProbeMetaEntry() { Type = MetaFieldType.Nothing, Length = 0};
		/// <summary>
		/// An entry for an opcode.
		/// </summary>
		public static ProbeMetaEntry Opcode = new ProbeMetaEntry() { Type = MetaFieldType.Opcode, Length = 1};

		/// <summary>
		/// The type of the field that was written.
		/// </summary>
		public MetaFieldType Type;
		/// <summary>
		/// Relevant field length.
		/// </summary>
		public ulong Length;
		
		/// <summary>
		/// Views this entry as a string.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return Type+"(" + Length + ")";
		}
	}
}