
namespace Api.Huddles
{
	/// <summary>
	/// Info about the user profile to return when they join a huddle.
	/// </summary>
	public partial struct HuddleJoinInfo
	{
		
		/// <summary>
		/// Display name to use.
		/// </summary>
		public string DisplayName;
		
		/// <summary>
		/// Avatar
		/// </summary>
		public string AvatarRef;
		
	}
}