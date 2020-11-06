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
		private Dictionary<int, bool> Lookup = new Dictionary<int, bool>();
		
		
		/// <summary>
		/// True if given ID is in the lookup.
		/// </summary>
		public bool Contains(int id)
		{
			return Lookup.ContainsKey(id);
		}
		
		/// <summary>
		/// Add content ID to lookup.
		/// </summary>
		public void Add(int id)
		{
			Lookup[id] = true;
		}
		
	}
	
}