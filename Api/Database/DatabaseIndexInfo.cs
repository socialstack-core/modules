using Api.AutoForms;
using Api.Startup;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Api.Database
{
	
	/// <summary>
	/// Information about a particular index.
	/// </summary>
	public partial class DatabaseIndexInfo
	{
		private static int counter = 1;

		/// <summary>
		/// The ID for this dbi.
		/// </summary>
		public int Id;

		/// <summary>
		/// The underlying columns in the index. Almost always just one.
		/// </summary>
		public ContentField[] Columns;
		
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
		public DatabaseIndexInfo(DatabaseIndexAttribute attr, ContentField[] fields)
		{
			Columns = fields;
			var sb = new StringBuilder();

			for (var i = 0; i < fields.Length; i++)
			{
				if (i != 0)
				{
					sb.Append('_');
				}
				sb.Append(fields[i].Name);
			}

			IndexName = sb.ToString();
			Unique = attr.Unique;
			Direction = attr.Direction;
		}

		private ChildIndexMeta _meta;

		/// <summary>
		/// Instances an index of this type. Note that the given type param, T, can only be one type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public ServiceCacheIndex<T> CreateIndex<T>()
			 where T : class
		{
			if (_meta == null)
			{
				_meta = BuildMeta(typeof(T));
			}

			return Activator.CreateInstance(_meta.ChildType, _meta.Meta) as ServiceCacheIndex<T>;
		}

		private ChildIndexMeta BuildMeta(Type contentType)
		{
			AssemblyName assemblyName = new AssemblyName("GeneratedIndex_" + counter);
			counter++;
			AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
			
			ChildIndexMeta indexMeta = null;

			TypeBuilder[] builders = new TypeBuilder[Columns.Length];

			var cim = new Type[] { typeof(ChildIndexMeta) };
			var cTypeArr = new Type[] { contentType };

			for (var i = Columns.Length - 1; i >= 0; i--)
			{
				var colInfo = Columns[i];
				Type baseType;
				var keyType = colInfo.FieldType;
				
				if (i == Columns.Length - 1)
				{
					// Last one - this is either a uniqueIndex or NonUniqueIndex.
					baseType = Unique ? typeof(UniqueIndex<,>).MakeGenericType(new Type[] {
						contentType,
						keyType
					}) : typeof(NonUniqueIndex<,>).MakeGenericType(new Type[] {
						contentType,
						keyType
					});
				}
				else
				{
					// An Index->Index.
					baseType = typeof(IndexIndex<,,>).MakeGenericType(new Type[] {
						contentType,
						keyType
					});
				}

				var baseCtor = baseType.GetConstructor(cim);

				// Create an inheriting type which reads the field as the key value:
				TypeBuilder typeBuilder = moduleBuilder.DefineType("IndexColumn_"+ i + "_" + colInfo.FieldInfo.Name, TypeAttributes.Public, baseType);
				
				// Main constructor accepts 1 arg, a ChildIndexMeta:
				ConstructorBuilder ctor0 = typeBuilder.DefineConstructor(
					MethodAttributes.Public,
					CallingConventions.Standard,
					cim
				);

				ILGenerator constructorBody = ctor0.GetILGenerator();
				constructorBody.Emit(OpCodes.Ldarg_0);
				constructorBody.Emit(OpCodes.Ldarg_1);
				constructorBody.Emit(OpCodes.Call, baseCtor);
				constructorBody.Emit(OpCodes.Ret);

				var writeBinary = typeBuilder.DefineMethod("GetKeyValue", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, keyType, cTypeArr);

				ILGenerator writerBody = writeBinary.GetILGenerator();
				writerBody.Emit(OpCodes.Ldarg_1);
				writerBody.Emit(OpCodes.Ldfld, colInfo.FieldInfo);
				writerBody.Emit(OpCodes.Ret);

				builders[i] = typeBuilder;
			}

			for (var i=Columns.Length - 1;i>=0;i--)
			{
				var meta = new ChildIndexMeta() { ChildType = builders[i].CreateType(), Meta = indexMeta };
				indexMeta = meta;
			}

			return indexMeta;
		}
	}
}