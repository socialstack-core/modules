using System;

namespace Api.Startup
{
	/// <summary>
	/// Indicates that your Content type is a cache only one.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	internal sealed class CacheOnlyAttribute : Attribute
	{
		/// <summary>
		/// True if IDs should be generated.
		/// This requires ContentSync as it uses its ID allocator. 
		/// Note that IDs are globally unique (i.e. two servers won't allocate the same ID) when this is active.
		/// </summary>
		public bool GenerateIds = true;
	}
}
