using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Huddles
{
	
	/// <summary>
	/// A HuddleLoadMetric
	/// </summary>
	public partial class HuddleLoadMetric : Entity<int>
	{
		/// <summary>
		/// 15 minute time block, based on projected durations of a huddle.
		/// </summary>
		[DatabaseIndex(Unique = false)]
		public int TimeSliceId;
		
		/// <summary>
		/// Assigned huddle server ID.
		/// </summary>
		public int HuddleServerId;
		
		/// <summary>
		/// Calculated load factor for a particular huddle server at this time slice.
		/// </summary>
		public int LoadFactor;
	}

}