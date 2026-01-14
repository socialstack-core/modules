using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Payments
{
	
	/// <summary>
	/// A Promotion
	/// </summary>
	public partial class Promotion : VersionedContent<uint>
	{
		
        /// <summary>
        /// The name of the promotion
        /// </summary>
        [DatabaseField(Length = 200)]
		public string Name;
		
		/// <summary>
		/// The content of this promotion.
		/// </summary>
		public JsonString BodyJson;

		/// <summary>
		/// The feature image ref
		/// </summary>
		[DatabaseField(Length = 80)]
		public string FeatureRef;
		
	}

}