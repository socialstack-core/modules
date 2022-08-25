using Api.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Api.ContentSync
{
	/// <summary>
	/// The latest ID in a particular stripe.
	/// </summary>
	public class LatestId
	{
		/// <summary>
		/// The latest ID in a particular segment.
		/// </summary>
		public ulong Id;
	}
	
	/// <summary>
	/// Generates long/ ulong IDs.
	/// </summary>
	public class IdAssigner<ID> where ID:struct
	{
		/// <summary>
		/// Assigns an ID of the given type.
		/// </summary>
		/// <returns></returns>
		public virtual ID Assign()
		{
			return default;
		}
	}

	/// <summary>
	/// Assigns IDs for a particular stripe.
	/// </summary>
	public class IdAssignerUInt32 : IdAssigner<uint>
	{
		private object _locker = new object();
		
		/// <summary>
		/// The max ID.
		/// </summary>
		public uint Current;

		/// <summary>
		/// Creates an ID assigner
		/// </summary>
		/// <param name="current"></param>
		public IdAssignerUInt32(uint current)
		{
			Current = current;
		}

		/// <summary>
		/// Gets the next ID in the sequence.
		/// </summary>
		/// <returns></returns>
		public override uint Assign()
		{
			uint result;
			
			lock(_locker){
				result = ++Current;
			}
			
			return result;
		}
	}

	/// <summary>
	/// Assigns IDs for a particular stripe.
	/// </summary>
	public class IdAssignerUInt64 : IdAssigner<ulong>
	{
		private object _locker = new object();
		
		/// <summary>
		/// The max ID.
		/// </summary>
		public ulong Current;

		/// <summary>
		/// Creates an ID assigner
		/// </summary>
		/// <param name="current"></param>
		public IdAssignerUInt64(ulong current)
		{
			Current = current;
		}

		/// <summary>
		/// Gets the next ID in the sequence.
		/// </summary>
		/// <returns></returns>
		public override ulong Assign()
		{
			ulong result;
			
			lock(_locker){
				result = ++Current;
			}
			
			return result;
		}

	}
}