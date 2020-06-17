using System;
using System.Collections;
using System.Collections.Generic;


namespace Api.Database{
	
	/// <summary>
	/// A section of results with a total.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ListWithTotal<T> {

		/// <summary>
		/// This particular segment of the results set.
		/// </summary>
		public List<T> Results;
		/// <summary>
		/// The total number of results if there was no pagination.
		/// This is null unless you explicitly ask for it via sending includeTotal:true, or if you're not using pagination in your request anyway.
		/// </summary>
		public int? Total;

	}
	
}
