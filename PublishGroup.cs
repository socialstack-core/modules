using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.PublishGroups
{
	
	/// <summary>
	/// A PublishGroup. Created and manipulated by users but doesn't have revisions itself.
	/// </summary>
	public partial class PublishGroup : UserCreatedContent<int>
	{
        /// <summary>
        /// The internal name of the group, used internally by admins only
        /// </summary>
        [DatabaseField(Length = 200)]
		public string Name;
	}

}