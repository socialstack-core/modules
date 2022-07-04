using System;
using System.Runtime.InteropServices;


namespace Api.SocketServerLibrary
{

	/// <summary>
	/// A rapid way for converting between raw bytes and a float by making a uint overlap a float.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size=4)]
	public struct FloatBits {
		/// <summary>
		/// The float value. Used to read the float value.
		/// </summary>
		[FieldOffset(0)]public float Float;
		/// <summary>
		/// The uint value. Used to read the bytes.
		/// </summary>
		[FieldOffset(0)]public uint Int;
		
		/// <summary>
		/// Sets up for a given float.
		/// </summary>
		/// <param name="v"></param>
		public FloatBits(float v):this(){
			Float=v;
		}
		
		/// <summary>
		/// Sets up for a given set of 4 bytes in a uint.
		/// </summary>
		/// <param name="v"></param>
		public FloatBits(uint v):this(){
			Int=v;
		}
	}

	/// <summary>
	/// A rapid way for converting between raw bytes and a double by making a ulong overlap a double.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 8)]
	public struct DoubleBits
	{
		/// <summary>
		/// The double value. Used to read the float value.
		/// </summary>
		[FieldOffset(0)] public double Double;
		/// <summary>
		/// The ulong value. Used to read the bytes.
		/// </summary>
		[FieldOffset(0)] public ulong Int;

		/// <summary>
		/// Sets up for a given double.
		/// </summary>
		/// <param name="v"></param>
		public DoubleBits(double v) : this()
		{
			Double = v;
		}

		/// <summary>
		/// Sets up for a given set of 8 bytes in a ulong.
		/// </summary>
		/// <param name="v"></param>
		public DoubleBits(ulong v) : this()
		{
			Int = v;
		}
	}
}