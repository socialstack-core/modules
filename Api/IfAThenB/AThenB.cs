using Api.Users;
using Api.AutoForms;


namespace Api.IfAThenB
{

	/// <summary>
	/// An if _ then _ rule.
	/// </summary>
	public partial class AThenB : VersionedContent<uint>
	{
		/// <summary>
		/// A name for the rule.
		/// </summary>
		public string Name;
		
		/// <summary>
		/// Optional description so people know what this rule does.
		/// </summary>
		public string Description;
		
		/// <summary>
		/// The graph runtime.
		/// </summary>
		[Module("Admin/CanvasEditor/GraphEditor")]
		[Data("type", "graph")]
		[Data("namespace", "Admin/IfAThenB/NodeSet/")]
		public string RuleGraphJson;
		
	}

}