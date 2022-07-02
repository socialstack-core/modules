using System.Collections.Generic;

namespace Api.CanvasRenderer
{
	
	/// <summary>
	/// When any of these exist the frontend will very clearly display them.
	/// </summary>
	public class PrebuiltMeta
	{
		/// <summary>
		/// True if this bundle needs the starter line.
		/// </summary>
		public bool Starter {get; set;}
		
		/// <summary>
		/// Timestamp in ms that the file was built at.
		/// </summary>
		public long BuildTime {get; set;}
		
		/// <summary>
		/// List of meta template literals.
		/// </summary>
		public List<PrebuiltTemplateLiteralMeta> Templates {get; set;}
	}
	
	
	/// <summary>
	/// Template literal metadata stored in meta.json when in prebuilt UI mode.
	/// </summary>
	public class PrebuiltTemplateLiteralMeta
	{
		/// <summary>
		/// Original module.
		/// </summary>
		public string Module {get; set;}
		
		/// <summary>
		/// Original template literal contents. Differs from target only when minified.
		/// </summary>
		public string Original {get; set;}
		
		/// <summary>
		/// The target template literal. Differs from original only when minified.
		/// </summary>
		public string Target {get; set;}
		
		/// <summary>
		/// Map of any expressions (specifically, simple variable form ones only).
		/// </summary>
		public Dictionary<string, string> VariableMap {get; set;}
		
		/// <summary>
		/// Start of the template literal, excluding the `.
		/// </summary>
		public int Start {get; set;}
		
		/// <summary>
		/// End of the template literal, excluding the `.
		/// </summary>
		public int End {get; set;}
	}
	
}