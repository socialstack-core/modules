using System;

namespace Api.Permissions
{
	/// <summary>
	/// Add Permissions(IsManual=true) to declare that you'll test the capability when you dispatch the event.
	/// You can get the capability from your event handler - use eventHandler.TestCapability to actually run the capability check.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	internal sealed class PermissionsAttribute : Attribute
	{
		/// <summary>
		/// True to indicate manual handling of permissions during event dispatch.
		/// </summary>
		public bool IsManual;
		/// <summary>
		/// True to indicate a field (or when on a class, all of the class fields unless they reverse it) are hidden during serialisation by default.
		/// "Default" is overridable per role by the permission system. Note that field visibility doesn't vary beyond role.
		/// </summary>
		public bool HideFieldByDefault;

		public PermissionsAttribute(){
		}
		
	}
}
