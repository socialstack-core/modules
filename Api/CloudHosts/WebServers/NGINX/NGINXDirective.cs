using System.Text;

namespace Api.CloudHosts;


/// <summary>
/// A class which represents a "directive" in NGINX config lingo.
/// Essentially a key/value pair which sets a particular piece of config. Some directives are order sensitive so they are a list not a dictionary.
///
///   error_log "/var/log/nginx/error.log";    -- a directive
///     ^                ^
///    key             value
/// </summary>
public partial class NGINXDirective
{
	/// <summary>
	/// The key (config variable name usually) of this directive.
	/// </summary>
	public string Key;
	
	/// <summary>
	/// The value of this directive.
	/// </summary>
	public string Value;
	
	
	/// <summary>
	/// Creates an empty NGINXDirective.
	/// </summary>
	public NGINXDirective()
	{}
	
	/// <summary>
	/// Creates a new NGINX directive with the given key/ value pair.
	/// </summary>
	public NGINXDirective(string key, string value = null)
	{
		Key = key;
		Value = value;
	}
	
	/// <summary>
	/// Efficient mechanism for stringifying this directive.
	/// Does not output ; as the presence of the semicolon varies based on where the directive is.
	/// </summary>
	public void ToString(StringBuilder builder, int depth = 0)
	{
		builder.Append(Key);
		builder.Append(' ');
		builder.Append(Value);
	}
}