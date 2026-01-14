using Api.Configuration;
using System.Collections.Generic;

namespace Api.Pages;


/// <summary>
/// The response of a page/state call.
/// </summary>
public partial struct PageStateResult
{
	/// <summary>
	/// True if the request originated from an old version and the page needs to be reloaded.
	/// </summary>
	public bool OldVersion;

	/// <summary>
	/// Optional redirect target.
	/// </summary>
	public string Redirect;

	/// <summary>
	/// Configuration.
	/// </summary>
	public Dictionary<string, Config> Config;

	/// <summary>
	/// The page itself.
	/// </summary>
	public Page Page;

	/// <summary>
	/// Token resolved description.
	/// </summary>
	public string Description;

	/// <summary>
	/// Token resolved title.
	/// </summary>
	public string Title;

	/// <summary>
	/// Primary object.
	/// </summary>
	public object Po;

	/// <summary>
	/// URL tokens.
	/// </summary>
	public List<string> TokenNames;

	/// <summary>
	/// URL token values.
	/// </summary>
	public List<string> Tokens;
}