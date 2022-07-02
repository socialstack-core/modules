namespace Api.Permissions
{

	/// <summary>
	/// Capabilities are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Capabilities
    {
		
		/// <summary>
		/// Ability to list all endpoints.
		/// </summary>
        public static Capability AvailableEndpointList;
		
    }
	
}
