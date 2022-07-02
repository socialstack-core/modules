using System;

namespace Api.SocketServerLibrary
{
	/// <summary>
	/// Much like the bolt equiv of JsonIgnore. This tells bolt to act like a field does not exist.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	internal sealed class BoltIgnoreAttribute : Attribute
	{
		public BoltIgnoreAttribute(){
		}
	}
}
