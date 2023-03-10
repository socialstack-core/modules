using System;
using System.Collections.Generic;

namespace Api.AutoForms
{
	/// <summary>
	/// Use this to define a fields order when rendering the field inside an autofrom.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	internal class OrderAttribute : Attribute
	{
		/// <summary>
		/// The order of this field in the auto form.
		/// </summary>
		public uint Order;
		
		public OrderAttribute(uint order)
		{
			Order = order;
		}

	}
}
