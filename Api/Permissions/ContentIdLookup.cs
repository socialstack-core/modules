using System;
using System.Collections;
using System.Collections.Generic;


namespace Api.Permissions{
	
	/// <summary>
	/// A collection of content IDs along with an optional content type ID.
	/// </summary>
	public class ContentIdLookup{
		
		/// <summary>
		/// The type ID of the content in this lookup.
		/// </summary>
		public int ContentTypeId;
		
		/// <summary>
		/// Used to find content by ID.
		/// </summary>
		private Dictionary<uint, bool> Lookup = new Dictionary<uint, bool>();
		
		
		/// <summary>
		/// True if given ID is in the lookup.
		/// </summary>
		public bool Contains(uint id)
		{
			return Lookup.ContainsKey(id);
		}
		
		/// <summary>
		/// Add content ID to lookup.
		/// </summary>
		public void Add(uint id)
		{
			Lookup[id] = true;
		}
		
	}
	
}