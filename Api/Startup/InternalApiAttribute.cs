using System;

namespace Api.Startup
{
	/// <summary>
	/// This can then be used to block sensitive classed from being processed
	/// For example the HtmlController from being exposed to the public via graphql and swagger etc 
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public sealed class InternalApiAttribute : Attribute
	{

	}
}
