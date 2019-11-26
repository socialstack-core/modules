using System;
using Api.Configuration;


namespace Api.Database
{
	/// <summary>
	/// Extensions to system.type for a convenient TableName method.
	/// </summary>
	public static class TypeExtensions
	{
		/// <summary>
		/// The table name to use for a particular type.
		/// This is generally used on types which are DatabaseRow instances.
		/// </summary>
		public static string TableName(this Type type)
		{
			// Just prefixed (e.g. sstack_Product by default):
			return AppSettings.DatabaseTablePrefix + type.Name.ToLower();
		}
		
	}
	
}