using System;

namespace Api.AutoForms
{
	/// <summary>
	/// Use this on fields either the endpoint model or the target model to block autoform from copying it automatically.
	/// Essentially this declares the field is definitely going to be mapped with custom handling in your form event.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
	internal sealed class DontCopyAttribute : Attribute
	{
	}
}
