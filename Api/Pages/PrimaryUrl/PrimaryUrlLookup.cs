using Api.Database;
using Stripe;
using System;
using System.Collections.Generic;

namespace Api.Pages;

/// <summary>
/// A lookup for a primary URL.
/// </summary>
public class PrimaryUrlLookup
{
	/// <summary>
	/// Adds the given URL as a primary one to this lookup, optionally with a specific content ID.
	/// </summary>
	/// <param name="url"></param>
	/// <param name="specificContentId"></param>
	public virtual void Add(string url, ulong specificContentId)
	{
		
	}

	/// <summary>
	/// Get the autoservice for this lookup.
	/// </summary>
	/// <returns></returns>
	public virtual AutoService GetService()
	{
		return null;
	}

}

/// <summary>
/// A lookup for a primary URL.
/// </summary>
public class PrimaryUrlLookup<T, ID> : PrimaryUrlLookup
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
{
	/// <summary>
	/// The autoservice that this lookup is for.
	/// </summary>
	public AutoService<T, ID> Service;

	/// <summary>
	/// A lookup for primary URL.
	/// </summary>
	/// <param name="svc"></param>
	public PrimaryUrlLookup(AutoService<T, ID> svc)
	{
		Service = svc;

	}
	/// <summary>
	/// The fallback if a specific ID does not exist in the lookup.
	/// </summary>
	public string FallbackUrl;
	
	/// <summary>
	/// A lookup by specific content ID.
	/// </summary>
	public Dictionary<ID, string> SpecificLookup;
	
	
	/// <summary>
	/// Gets the URL for a specific piece of content.
	/// </summary>
	public string GetUrl(T content)
	{
		if(SpecificLookup != null)
		{
			if(SpecificLookup.TryGetValue(content.Id, out string val))
			{
				return val;
			}
		}
		
		// Todo: FallbackUrl can contain tokens: need to render them out.
		
		return FallbackUrl;
	}

	/// <summary>
	/// Adds the given URL as a primary one to this lookup, optionally with a specific content ID.
	/// </summary>
	/// <param name="url"></param>
	/// <param name="specificContentId"></param>
	public override void Add(string url, ulong specificContentId)
	{
		if (specificContentId == 0)
		{
			// todo: this URL can contain tokens
			FallbackUrl = url;
			return;
		}

		if (SpecificLookup == null)
		{
			SpecificLookup = new Dictionary<ID, string>();
		}

		SpecificLookup[Service.ConvertId(specificContentId)] = url;
	}

	/// <summary>
	/// Get the autoservice for this lookup.
	/// </summary>
	/// <returns></returns>
	public override AutoService GetService()
	{
		return Service;
	}

}