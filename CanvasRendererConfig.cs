
namespace Api.CanvasRenderer
{
	
	/// <summary>
	/// Config for the canvas renderer service.
	/// </summary>
	public partial class CanvasRendererServiceConfig
	{
		
		/// <summary>
		/// The module set to use when rendering canvases. Either "Admin", "Email" or "UI". 
		/// The default is "Admin" as it always includes the modules from the other 2 sets.
		/// </summary>
		public string Modules { get; set; } = "Admin";
		
		/// <summary>
		/// True if you'd like to use react.
		/// </summary>
		public bool UseReact { get; set; } = false;
		
	}
	
}