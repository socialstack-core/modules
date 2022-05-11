using Api.BlockDatabase;
using Lumity.BlockChains;
using System.Reflection;

namespace Api.Startup
{

	/// <summary>
	/// ContentField extensions
	/// </summary>
	public partial class ContentField
	{
		/// <summary>
		/// A delegate used to write chain fields.
		/// </summary>
		public WriteChainField FieldWriter;

		/// <summary>
		/// True if the chain data type is unsigned.
		/// </summary>
		public bool IsUnsigned;

		/// <summary>
		/// True if this field goes on the private chain.
		/// </summary>
		public bool IsPrivate;

		/// <summary>
		/// The underlying MethodInfo for the field writer function. 
		/// </summary>
		public MethodInfo FieldWriterMethodInfo;

		/// <summary>
		/// The data type of this field on the chain.
		/// </summary>
		public string DataType;

		/// <summary>
		/// The field definition on the chain. Null during initial setup; this only is guaranteed to exist once the schema has been updated.
		/// </summary>
		public FieldDefinition Definition;

		/// <summary>
		/// A delegate used to read chain fields.
		/// </summary>
		public ReadChainField FieldReader;
	}

}