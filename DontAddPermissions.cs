using System;

namespace Api.Permissions
{
	/// <summary>
	/// Use this on specifically event handlers to avoid permissions being added.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
	internal sealed class DontAddPermissionsAttribute : Attribute
	{
	}
}
