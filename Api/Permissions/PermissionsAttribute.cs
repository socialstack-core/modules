using System;

namespace Api.Permissions
{
	/// <summary>
	/// Add Permissions(IsManual=true) to declare that you'll test the capability when you dispatch the event.
	/// You can get the capability from your event handler - use eventHandler.TestCapability to actually run the capability check.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	internal sealed class PermissionsAttribute : Attribute
	{
		/// <summary>
		/// True to indicate manual handling of permissions during event dispatch.
		/// </summary>
		public bool IsManual;

		/// <summary>
		/// True if you want this field to be hidden. Setting this to true is the same as setting the (visibility) Rule to false.
		/// Specifying Roles will restrict it to those particular roles.
		/// </summary>
		public bool Hide
		{
			get
			{
				return Rule == "false";
			}
			set 
			{
				// Intentionally upside down as the rule is for visibility.
				Rule = value ? "false" : "true";
			}
		}

		/// <summary>
		/// True if this non-content type event handler should register a common capability handler and test it when the event handler triggers. 
		/// </summary>
		public bool Check;

		/// <summary>
		/// A convenience mechanism for setting both read and write rules at the same time.
		/// Optionally specify Roles to restrict it to specifically those roles.
		/// </summary>
		public string Rule
		{
			get
			{
				return ReadRule;
			}
			set
			{
				ReadRule = value;
				WriteRule = value;
			}
		}

		/// <summary>
		/// Combined with any of the Rule/Hide fields to set which role(s) the rule will be applied to. This is the role keys comma separated.
		/// Leaving it on null is the same as Roles="*". There is one special keyword - "admins" - which means "all CanViewAdmin roles".
		/// You can also use ! to exclude a role as well. For example Roles="*,!member" meaning every role except member.
		/// Note that the last matching rule wins: "!member,*" will be true for member as the * test comes later.
		/// </summary>
		public string Roles;

		/// <summary>
		/// A filter rule that runs when the field is being read (emitted to JSON). Use this to prevent the field from being exposed to specific users.
		/// Optionally specify Roles to restrict it to specifically those roles.
		/// </summary>
		public string ReadRule;

		/// <summary>
		/// A filter rule that runs when the field is being written to (read from JSON). Use this to prevent the field from being exposed to specific users.
		/// Optionally specify Roles to restrict it to specifically those roles.
		/// </summary>
		public string WriteRule;

		public PermissionsAttribute(){
		}
		
	}
}
