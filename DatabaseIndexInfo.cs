using Api.AutoForms;
using System;
using System.Reflection;

namespace Api.Database
{
	
	/// <summary>
	/// Information about a particular index.
	/// </summary>
	public partial class DatabaseIndexInfo
	{
		/// <summary>
		/// The columns in the index.
		/// </summary>
		public string[] Columns;
		
		/// <summary>
		/// The underlying fieldInfo for the columns.
		/// </summary>
		public FieldInfo[] ColumnFields;
		
		/// <summary>
		/// A generated index name.
		/// </summary>
		public string IndexName;
		
		/// <summary>
		/// True if it's a unique index. True is the default.
		/// </summary>
		public bool Unique;
		
		/// <summary>
		/// either "ASC" or "DESC" declaring the sort direction of the index. ASC is the default.
		/// </summary>
		public string Direction;
		
		
		/// <summary>
		/// Creates index info based on the given class attribute.
		/// It's expected to define the column names.
		/// </summary>
		public DatabaseIndexInfo(DatabaseIndexAttribute attr, Type type)
		{
			Columns = attr.Fields;
			
			if(Columns == null){
				throw new ArgumentException("You've got a [DatabaseIndex] on " + ((type == null) ? "a type" : type.Name) + " which requires fields but has none.");
			}
			
			ColumnFields = new FieldInfo[Columns.Length];
			
			for (var i=0; i<Columns.Length; i++) {
				ColumnFields[i] = type.GetField(Columns[i], BindingFlags.Public | BindingFlags.Instance);
			}


		}
		
		public DatabaseIndexInfo(DatabaseIndexAttribute attr, FieldInfo field)
		{
			ColumnFields = new FieldInfo[]{field};
			Columns = new string[]{field.Name};
			IndexName = field.Name;
			Unique = attr.Unique;
		}
	}
}