using System;
using Api.Database;
using Api.DatabaseDiff;


namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{

		#region Service events

		/// <summary>
		/// This is invoked just before database diff adds the given type to the schema.
		/// Use it to add additional types, or block adding this one.
		/// </summary>
		public static EventHandler<FieldMap, Type, Schema> DatabaseDiffBeforeAdd;

		/// <summary>
		/// This is invoked just after database diff adds the given type to the schema and the table now exists.
		/// </summary>
		public static EventHandler<DiffSet<DatabaseTableDefinition, DiffSet<DatabaseColumnDefinition, ChangedColumn>>> DatabaseDiffAfterAdd;

		#endregion

	}

}
