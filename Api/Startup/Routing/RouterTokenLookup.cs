using System;
using System.Collections.Generic;

namespace Api.Startup.Routing;


/// <summary>
/// Very similar to the C# string heap.
/// Used for static routing of known token values.
/// An example of when such a known token value happens is a permalink from e.g. /homeware/hello-world/product-key/ to /product/42
/// The '42' is a static known token value, as the page is using a token i.e. /product/{product.id} and the 42 is actually in the routing system.
/// </summary>
public static class RouterTokenLookup
{
	
	private static Dictionary<string, int> _reverseLookup = new Dictionary<string, int>();
	
	private static object _lock = new object();
	
	private static int _fill = 0;
	
	private static string[] _lookup = new string[10];
	
	/// <summary>
	/// Add a token to the lookup, returning its index.
	/// Thread safe: can be called without interrupting ongoing Get traffic.
	/// </summary>
	public static int Add(string token)
	{
		if(_reverseLookup.TryGetValue(token, out int index))
		{
			return index;
		}
		
		lock(_lock)
		{
			index = _fill++;
			
			if(index >= _lookup.Length)
			{
				Array.Resize(ref _lookup, _lookup.Length + 50);
			}
		}
		
		_lookup[index] = token;
		_reverseLookup[token] = index;
		return index;
	}
	
	/// <summary>
	/// Gets an entry from the lookup at the given index.
	/// </summary>
	public static string Get(int index)
	{
		return _lookup[index];
	}
	
}