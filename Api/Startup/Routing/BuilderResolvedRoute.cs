using System.Collections.Generic;

namespace Api.Startup.Routing;


/// <summary>
/// A URL resolved by the route builder.
/// Slower than the main built router but is used for pre-resolution of route rewrites.
/// </summary>
public struct BuilderResolvedRoute
{

	/// <summary>
	/// The node that resolving this path will get to. Not necessarily the terminal point.
	/// </summary>
	public BuilderNode Node;

	/// <summary>
	/// Any tokens collected along the way.
	/// </summary>
	public List<string> Tokens;

}