namespace Api.SocketServerLibrary
{
	/// <summary>
	/// A frame on the read stack. Tracks the state of the data read head.
	/// </summary>
	public struct RecvStackFrame
	{
		/// <summary>
		/// Current phase number.
		/// </summary>
		public int Phase;

		/// <summary>
		/// number of bytes required for this frame to be able to process anything.
		/// </summary>
		public int BytesRequired;

		/// <summary>
		/// The reader which will process the available bytes.
		/// </summary>
		public MessageReader Reader;

		/// <summary>
		/// Target object, if there is one.
		/// </summary>
		public object TargetObject;
	}
}