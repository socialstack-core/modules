using Api.Permissions;


namespace Api.Contexts
{
	/// <summary>
	/// Stores info about publicly settable context fields.
	/// </summary>
	public partial class ContextFieldInfo
	{
		/// <summary>
		/// The capability which indicates if the field can be loaded.
		/// </summary>
		public Capability ViewCapability;
	}
}