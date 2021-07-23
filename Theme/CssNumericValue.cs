using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;


namespace Api.Themes
{
	
	/// <summary>
	/// Represents a numeric value - either an int or %.
	/// </summary>
	
	public struct CssNumericValue : IEquatable<CssNumericValue>
	{
		/// <summary>
		/// not defined.
		/// </summary>
		public static CssNumericValue None = new CssNumericValue(-1f, false);

		/// <summary>
		/// The underlying value
		/// </summary>
		public float Value;
		/// <summary>
		/// True if the value is a percent.
		/// </summary>
		public bool IsPercent;
		
		/// <summary>
		/// Creates a numeric value, optionally marked as a %.
		/// </summary>
		public CssNumericValue(float v, bool isPercent)
		{
			Value = v;
			IsPercent = isPercent;
		}

		/// <summary>
		/// Gets this numeric value as a percent.
		/// </summary>
		/// <param name="divisor"></param>
		/// <returns></returns>
		public float ToPercent(float divisor)
		{
			if (IsPercent)
			{
				return Value;
			}

			return Value / divisor;
		}

		/// <summary>
		/// True if this value equals another.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			return obj is CssNumericValue c && this == c;
		}

		/// <summary>
		/// Equality
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(CssNumericValue other)
		{
			return Value == other.Value && IsPercent == other.IsPercent;
		}

		/// <summary>
		/// Gets the value hashcode.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return Value.GetHashCode() + (IsPercent ? 1 : 0);
		}

		/// <summary>
		/// Equals
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public static bool operator ==(CssNumericValue x, CssNumericValue y)
		{
			return x.Value == y.Value && x.IsPercent == y.IsPercent;
		}

		/// <summary>
		/// Not equals
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public static bool operator !=(CssNumericValue x, CssNumericValue y)
		{
			return x.Value != y.Value || x.IsPercent != y.IsPercent;
		}

	}
	
}