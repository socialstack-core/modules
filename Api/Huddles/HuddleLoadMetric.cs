using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Huddles
{
	
	/// <summary>
	/// A HuddleLoadMetric
	/// </summary>
	public partial class HuddleLoadMetric : Content<uint>
	{
		/// <summary>
		/// 15 minute time block, based on projected durations of a huddle.
		/// </summary>
		[DatabaseIndex(Unique = false)]
		public uint TimeSliceId;
		
		/// <summary>
		/// Assigned huddle server ID.
		/// </summary>
		public uint HuddleServerId;
		
		/// <summary>
		/// The huddle servers region.
		/// </summary>
		public uint RegionId;
		
		/// <summary>
		/// Calculated load factor for a particular huddle server at this time slice.
		/// </summary>
		public int LoadFactor;
	}

}