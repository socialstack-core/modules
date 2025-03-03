using Api.Contexts;
using Newtonsoft.Json.Linq;
using System.Reflection.Emit;
using System;
using System.Threading.Tasks;
using Api.Startup;
using Api.SocketServerLibrary;
using System.Reflection;
using Api.Database;

namespace Api.CanvasRenderer
{
	/// <summary>
	/// A set of fields.
	/// </summary>
	public class Fields : Executor
	{
		private Type _srcType;

		/// <summary>
		/// Used to serialise a complete object. Is a TypeReaderWriter[T].
		/// </summary>
		public object _typeReadWrite;

		/// <summary>
		/// Creates a set of fields from the info in the given JSON token.
		/// </summary>
		/// <param name="d"></param>
		public Fields(JToken d) : base(d)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="compileEngine"></param>
		public override async ValueTask Compile(NodeLoader compileEngine)
		{
			// Simply extracts fields from input object.
			// Nothing to actually emit in here - it all happens in the read field segment.

			var inputType = GetLinkedInputType("object");
			_srcType = inputType;
			var svc = Services.GetByContentType(inputType);

			if (svc != null)
			{
				_fields = svc.GetContentFields();
			}

			// Invoke SetupWriter to set the _typeReadWrite field.
			var setupWriterMethod = GetType().GetMethod(nameof(SetupWriter));

			var setupWriter = setupWriterMethod.MakeGenericMethod(new Type[] {
					svc.ServicedType,
					svc.IdType
				});

			await (ValueTask)(setupWriter.Invoke(this, new object[] {
				svc
			}));
		}

		/// <summary>
		/// Sets up the JSON writer for a specific identified service during the compile pass.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="svc"></param>
		/// <returns></returns>
		public async ValueTask SetupWriter<T, ID>(AutoService<T, ID> svc)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			var jsonStructure = await svc.GetTypedJsonStructure(new Context());

			if (jsonStructure.TypeIO == null)
			{
				jsonStructure.TypeIO = TypeIOEngine.Generate(jsonStructure);
			}

			_typeReadWrite = jsonStructure.TypeIO;
		}

		private ContentFields _fields;

		/// <summary>
		/// Returns type of a named output field.
		/// </summary>
		/// <param name="field"></param>
		public override Type GetOutputType(string field)
		{
			if (field == "output")
			{
				// Whole object.
				return _srcType;
			}

			if (_fields == null || !_fields.TryGetValue(field, out ContentField fld))
			{
				throw new Exception("Field '" + field + "' not found on type");
			}

			return fld.FieldType;
		}

		/// <summary>
		/// Emits JSON in to the datamap for an outputted field.
		/// </summary>
		/// <param name="compileEngine"></param>
		/// <param name="field"></param>
		public override void EmitOutputJson(NodeLoader compileEngine, string field)
		{
			if (_fields == null || !_fields.TryGetValue(field, out ContentField fld))
			{
				throw new Exception("Field '" + field + "' not found on type");
			}

			if (field == "output")
			{
				// Using a full JSON write here.
				// _typeReadWriter.WriteJsonUnclosed(T obj, Writer writer)

				// _typeReadWriter
				compileEngine.EmitCurrentNode();

				var typeReadWriteField = GetType().GetField("_typeReadWrite", BindingFlags.Public | BindingFlags.Instance);

				compileEngine.CodeBody.Emit(OpCodes.Ldfld, typeReadWriteField);

				// The object to serialise:
				compileEngine.EmitLoadInput("object", this);

				// Writer to put it in:
				compileEngine.EmitWriter();

				// Invoke the write method:
				var writeJsonMethod = _typeReadWrite.GetType().GetMethod("WriteJsonUnclosed");
				compileEngine.CodeBody.Emit(OpCodes.Callvirt, writeJsonMethod);

				// It's unclosed so next emit a write of the closing bracket.
				compileEngine.EmitWriteByte((byte)'}');

				return;
			}

			// Write a singular field:
			TypeIOEngine.EmitWriteField(compileEngine.CodeBody, fld, (ILGenerator body) => {

				// Load the input:
				compileEngine.EmitLoadInput("object", this);

			});
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="compileEngine"></param>
		/// <param name="field"></param>
		/// <exception cref="NotImplementedException"></exception>
		public override Type EmitOutputRead(NodeLoader compileEngine, string field)
		{
			// Load the src object:
			compileEngine.EmitLoadInput("object", this);

			if (field == "output")
			{
				// Whole object. Do nothing else here.
				return _srcType;
			}

			if (_fields == null || !_fields.TryGetValue(field, out ContentField fld))
			{
				throw new Exception("Field '" + field + "' not found on type");
			}

			if (fld.PropertyInfo != null)
			{
				// Load from the field using the src object on the stack currently:
				compileEngine.CodeBody.Emit(OpCodes.Callvirt, fld.PropertyInfo.GetGetMethod());
			}
			else if (fld.FieldInfo != null)
			{
				// Load from the field, using the src object on the stack currently:
				compileEngine.CodeBody.Emit(OpCodes.Ldfld, fld.FieldInfo);
			}
			else
			{
				throw new NotImplementedException("Can only use non-virtual fields in this Fields node on the server");
			}

			return fld.FieldType;
		}

	}
}
