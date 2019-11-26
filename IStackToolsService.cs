using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.StackTools
{
	/// <summary>
	/// This service is used to invoke the socialstack command line tools (node.js) 
	/// which e.g. build/ serverside render the UI and render emails etc.
	/// If this bottlenecks then it could potentially be swapped out for edge.js instead.
	/// </summary>
	public partial interface IStackToolsService
	{

		/// <summary>
		/// Get node.js to do something via sending it a serialisable request.
		/// </summary>
		/// <param name="serialisableMessage"></param>
		/// <param name="onResult">This callback runs when it responds.</param>
		void Request(object serialisableMessage, OnStackToolsResponse onResult);

		/// <summary>
		/// Get node.js to do something via sending it a raw json request.
		/// </summary>
		/// <param name="json"></param>
		/// <param name="onResult">This callback runs when it responds.</param>
		void RequestJson(string json, OnStackToolsResponse onResult);

	}
}
