using Api.Database;

namespace Api.Users{
	
	public partial class User
	{
		
		/// <summary>
		/// The user's email address.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string Email;

		/// <summary>
		/// Opt-out flags for different types of email.
		/// Currently 1=Essential, 2=Marketing.
		/// </summary>
		public uint EmailOptOutFlags;

		/// <summary>
		/// Token used to verify the email of the user.
		/// </summary>
		[DatabaseField(Length = 40)]
		[Newtonsoft.Json.JsonIgnore]
		public string EmailVerifyToken;

	}

}