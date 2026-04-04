using System;
using System.Collections.Generic;

namespace Api.AutoForms
{
	/// <summary>
	/// Use this to define a fields order when rendering the field inside an autofrom.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	internal class AdminSearchableAttribute : Attribute
	{
		/// <summary>
		/// True if searchable
		/// </summary>
		public bool Searchable;
		
		public AdminSearchableAttribute(bool searchable = true)
		{
			Searchable = searchable;
		}

	}

	/// <summary>
	/// The filter operator to use.
	/// </summary>
	public enum AdminSearchMode : int
	{
		/// <summary>
		/// Not searchable at all
		/// </summary>
		None = 0,
		/// <summary>
		/// Uses the "=?" filter op
		/// </summary>
		Equal = 1,
		/// <summary>
		/// Uses the "contains ?" filter op
		/// </summary>
		Contains = 2,
	}
}
