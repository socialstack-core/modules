using System;

namespace Api.Startup
{
	/// <summary>
	/// Use this attribute to make sure your service is instanced before others.
	/// The default priority is 10, and a lower number means being instanced sooner.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
	internal sealed class LoadPriorityAttribute : Attribute
	{
		/// <summary>
		/// The priority.
		/// </summary>
		public int Priority = 10;
		
		public LoadPriorityAttribute(int priority)
		{
			Priority = priority;
		}
	}
}
