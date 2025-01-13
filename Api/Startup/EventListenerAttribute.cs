using System;

namespace Api.Startup
{
	/// <summary>
	/// Use this in your module to be able to hook up to early 
	/// events such as the ones that happen during service configure.
	/// These are always instanced during startup.
	/// Note that services themselves are also instanced during startup so you can also hook up events 
	/// from their constructors (just not the particularly early events).
	/// Note that Services.Get is unavailable during the constructor - 
	/// you'll need to use the Services.OnStart event to grab services.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public sealed class EventListenerAttribute : Attribute
	{

	}
}
