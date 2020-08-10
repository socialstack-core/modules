using Api.Huddles;

namespace Api.Eventing
{
	/// <summary>
	/// Extensions for huddle invite events.
	/// </summary>
	public class HuddlePermittedUserEventGroup : EventGroup<HuddlePermittedUser>
	{
		/// <summary>
		/// Before accepting.
		/// </summary>
		public EventHandler<HuddlePermittedUser> BeforeAccept;
		/// <summary>
		/// After accepting.
		/// </summary>
		public EventHandler<HuddlePermittedUser> AfterAccept;
		/// <summary>
		/// Before cancelling (rejecting, or someone else accepted the same invite).
		/// </summary>
		public EventHandler<HuddlePermittedUser> BeforeCancel;
		/// <summary>
		/// After cancelling (rejecting, or someone else accepted the same invite).
		/// </summary>
		public EventHandler<HuddlePermittedUser> AfterCancel;
		
	}
}