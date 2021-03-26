using System;

namespace Api.Startup
{
	/// <summary>
	/// Indicates that your Content type is a cache only one.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	internal sealed class CacheOnlyAttribute : Attribute
	{

	}
}
