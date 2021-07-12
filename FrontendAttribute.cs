using System;

namespace Api.Configuration
{
	/// <summary>
	/// Add [Frontend] attributes to your config properties (or the whole config class) to declare that the config values should be made available to the frontend.
	/// You can access the values via useConfig from UI/Session.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	internal sealed class FrontendAttribute : Attribute
	{
		public FrontendAttribute(){
		}
		
	}
}
