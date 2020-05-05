using Api.Database;
using Newtonsoft.Json;

namespace Api.Users
{
	public partial class User
	{
		/// <summary>
		/// The users notification key
		/// </summary>
		[DatabaseField(Length = 400)]
		public string NotificationKey;

		/// <summary>
		/// The users notification key type
		/// </summary>
		[DatabaseField(Length = 10)]
		public string NotificationKeyType;
		
		/// <summary>
		/// True if notifs are disabled for this user.
		/// </summary>
		public bool QuietMode;
	}
}