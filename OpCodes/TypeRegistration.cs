using Api.SocketServerLibrary;
using System.Collections.Generic;
using System.Net;


namespace Api.ContentSync
{
	/// <summary>
	/// Registers a content type.
	/// </summary>
	public class TypeRegistration : Message
	{
		/// <summary>
		/// The content type ID.
		/// </summary>
		public int ContentTypeId;
		
		/// <summary>
		/// The opcode being listened for with a body of LocaleId, action (delete, update etc), then the object.
		/// </summary>
		public uint OpCodeToListenFor;

		/// <summary>
		/// The fields information.
		/// </summary>
		public List<string> FieldInfo;
	}
}