
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Api.Startup
{
	/// <summary>
	/// ID converter. Turns a ulong into a specific ID type.
	/// </summary>
	public class IDConverter<ID> where ID:struct, IEquatable<ID>, IComparable<ID>
	{

		/// <summary>
		/// Converts the given input ulong into an ID of the primary type.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public virtual ID Convert(ulong input)
		{
			return default(ID);
		}

	}

	/// <summary>
	/// uint32 conversion
	/// </summary>
	public class UInt32IDConverter : IDConverter<uint>
	{
		/// <summary>
		/// Converts the given input ulong into an ID of the primary type.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public override uint Convert(ulong input)
		{
			return (uint)input;
		}
	}


	/// <summary>
	/// uint64 conversion
	/// </summary>
	public class UInt64IDConverter : IDConverter<ulong>
	{
		/// <summary>
		/// Converts the given input ulong into an ID of the primary type.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public override ulong Convert(ulong input)
		{
			return input;
		}
	}

}