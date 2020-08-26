using Api.SocketServerLibrary;
using System.Net;


namespace Api.ContentSync
{
	/// <summary>
	/// Registers a content type.
	/// </summary>
	public class ContentUpdate<T> : Message
	{
		/// <summary>
		/// The action. 1=Created, 2=Updated, 3=Deleted.
		/// </summary>
		public byte Action;
		/// <summary>
		/// Locale of user who did it.
		/// </summary>
		public int LocaleId;
		/// <summary>
		/// User who did it.
		/// </summary>
		public int User;
		/// <summary>
		/// Role of user who did it.
		/// </summary>
		public int RoleId;
		
		/// <summary>
		/// The actual content.
		/// </summary>
		public T Content;
	}
}