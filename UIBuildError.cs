namespace Api.CanvasRenderer{
	
	/// <summary>
	/// When any of these exist the frontend will very clearly display them.
	/// </summary>
	public class UIBuildError{
		
		/// <summary>
		/// Pretty title
		/// </summary>
		public string Title;
		
		/// <summary>
		/// File path of the file that failed to build.
		/// </summary>
		public string File;
		
		/// <summary>
		/// Description if there is one.
		/// </summary>
		public string Description;
	}
	
}