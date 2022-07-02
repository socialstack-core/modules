using Api.Permissions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.UserFlags
{
	/// <summary>
	/// Implement to mark your thing as user flaggable.
	/// </summary>
	public partial interface IAmFlaggable
    {
		/// <summary>
		/// Total flag count on the content.
		/// </summary>
		[JsonIgnore]
		int UserFlagCount {get; set;}
	}
}
