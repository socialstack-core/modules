namespace Api.Users
{
	/// <summary>
	/// Used to auto install component groups.
	/// </summary>
	public class GroupBuilder : ContentBuilder
	{
		/// <summary>
		/// The name of the component group.
		/// </summary>
		public string Name;

		/// <summary>
		/// Array of allowed component rules.
		/// </summary>
		public string[] AllowedComponents;
	}
}
