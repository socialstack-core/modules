using Api.Contexts;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Api.CanvasRenderer
{
	/// <summary>
	/// A counter.
	/// </summary>
    public class Count : Executor
    {
		/// <summary>
		/// 
		/// </summary>
		private FieldBuilder _targetField;

		/// <summary>
		/// Create a new counter node from the given JSON token.
		/// </summary>
		/// <param name="d"></param>
        public Count(JToken d) : base(d)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compileEngine"></param>
        public override ValueTask Compile(NodeLoader compileEngine)
		{
			// For the stfld later:
			compileEngine.EmitLoadState();
               
            // Put array on stack:
			var inputType = compileEngine.EmitLoadInput("listOfItems", this);

			if (inputType.IsArray)
            {
                // Get length from array, put length on stack:
                var arrLen = inputType.GetProperty("Length").GetGetMethod();
                compileEngine.CodeBody.Emit(OpCodes.Call, arrLen);
            }
            else
            {
                // Lists etc todo
                throw new NotImplementedException("Can only use count node on arrays (not lists yet)");
            }

            _targetField = compileEngine.DefineStateField(typeof(int));

            // Store into state field - uses the number and the state currently on the stack.
            compileEngine.CodeBody.Emit(OpCodes.Stfld, _targetField);

			return new ValueTask();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="compileEngine"></param>
		/// <param name="field"></param>
		/// <exception cref="NotImplementedException"></exception>
		public override Type EmitOutputRead(NodeLoader compileEngine, string field)
		{
			// Load the state:
			compileEngine.EmitLoadState();

			// Load from the state field:
			compileEngine.CodeBody.Emit(OpCodes.Ldfld, _targetField);

			return typeof(int);
		}

		/// <summary>
		/// Emits JSON in to the datamap for an outputted field.
		/// </summary>
		/// <param name="compileEngine"></param>
		/// <param name="field"></param>
		public override void EmitOutputJson(NodeLoader compileEngine, string field)
		{
			// Output a number. Is simply WriteS-ed to the writer.
			compileEngine.EmitWriter();
			EmitOutputRead(compileEngine, field);
			compileEngine.EmitWriteSCall();
		}

		/*
		public override async Task<dynamic> Go(Context context, PageState pageState)
		{
			var listOfItmes = await ReadValue(context, pageState, "listOfItmes");

			if (listOfItmes != null)
			{
				if (listOfItmes is Array)
				{
					var arrayOfItems = listOfItmes as Array;
					return arrayOfItems.Length;
				}
				else if (listOfItmes is JArray)
				{
					var jArray = listOfItmes as JArray;
					return jArray.Count;
				}
				else
				{
					try
					{
						// Attempt to convert the value back to is json form
						JArray jArray = JArray.FromObject(listOfItmes);

						if (jArray != null)
						{
							return jArray.Count;
						}
					}
					catch (Exception e)
					{
						return 0;
					}
				}
			}

			return 0;
		}
		*/
	}
}
