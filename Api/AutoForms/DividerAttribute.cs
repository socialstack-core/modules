using System;

namespace Api.AutoForms
{
	/// <summary>
	/// Use this to render a horizontal rule directly after a given field.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
	internal sealed class DividerAttribute : Attribute
	{
	}
}
