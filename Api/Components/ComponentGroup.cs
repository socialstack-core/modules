using Api.AutoForms;
using Api.Database;
using Api.Permissions;
using Api.Startup;
using Api.Translate;
using Api.Users;


namespace Api.Components
{
	
	/// <summary>
	/// A ComponentGroup
	/// </summary>
	public partial class ComponentGroup : InstallableContent<uint>
	{
        /// <summary>
        /// The name of the component group
        /// </summary>
        [DatabaseField(Length = 200)]
		[Data("required", true)]
		public Localized<string> Name;

		/// <summary>
		/// a flat string[] json array of components a role is allowed to access
		/// </summary>
		[Module("Admin/ComponentGroup")]
		public string AllowedComponents;
	}

}
