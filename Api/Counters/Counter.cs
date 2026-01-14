using Api.Database;

namespace Api.Counters
{
	
	/// <summary>
	/// A store for thread-safe atomic Counters
	/// </summary>
	public partial class Counter : Content<uint>
	{
		/// <summary>
		/// The counter Id.
		/// </summary>
		public string _id;

		/// <summary>
		/// The current sequence number will be autoupdated FindOneAndUpdateAsync
		/// </summary>
		public long Sequence;

	}
}