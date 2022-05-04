using Api.SocketServerLibrary;
using System;


namespace Api.Database
{
	/// <summary>
	/// Maps e.g. tags to particular content type. Often Mapping[uint, uint].
	/// If it's e.g. a list of tags on a user, it has a UserId field and a TagId field, which are linked up to SourceId and TargetId.
	/// These are fully automated and used via the include system.
	/// </summary>
	[DatabaseIndex(false, "SourceId")]
	public class Mapping<S,T> : Content<uint> 
		where S : struct
		where T : struct
	{
		/// <summary>
		/// The UTC creation date. Read/ delete only rows so an edited date isn't present here.
		/// </summary>
		public DateTime CreatedUtc;

		/// <summary>
		/// Source ID.
		/// </summary>
		public S SourceId;

		/// <summary>
		/// Target ID.
		/// </summary>
		public T TargetId;
	}
}