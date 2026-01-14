namespace Api.Startup;


/// <summary>
/// Flags for driving special behaviours inside the serialiser.
/// These are mainly used by permission functionality.
/// </summary>
public enum ContextFlags : int {
	/// <summary>
	/// No flags set
	/// </summary>
	None = 0,
	/// <summary>
	/// True if the object was obtained via an inclusion. Do not change this from 1 without changing FilterAst (it has a built in optimisation).
	/// </summary>
	IsIncluded = 1,
	/// <summary>
	/// True if it is currently serialising primary content.
	/// </summary>
	IsPrimary = 2,
	/// <summary>
	/// Set if it is graph content.
	/// </summary>
	IsGraph = 4,
	/// <summary>
	/// Set if it is page primary content.
	/// </summary>
	IsPage = 8,
	/// <summary>
	/// Set if it is email primary content.
	/// </summary>
	IsEmail = 16
}