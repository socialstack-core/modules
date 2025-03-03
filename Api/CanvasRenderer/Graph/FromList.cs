using Api.Contexts;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Api.CanvasRenderer
{
    /// <summary>
    /// A list of fields from an object.
    /// </summary>
    public class FromList : Executor
    {
		private FieldBuilder _targetField;

        /// <summary>
        /// Creates a new list using data in the given JSON token.
        /// </summary>
        /// <param name="d"></param>
		public FromList(JToken d) : base(d)
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

            // Put index on stack:
            var indexType = compileEngine.EmitLoadInput("index", this);

            Type outputType;
            
			if (inputType.IsArray)
			{
                outputType = inputType.GetElementType();

				// Read from the array:
				compileEngine.CodeBody.Emit(OpCodes.Ldind_Ref, outputType);
			}
			else
			{
				// Lists etc todo
				throw new NotImplementedException("Can only use count node on arrays (not lists yet)");
			}

			_targetField = compileEngine.DefineStateField(outputType);

			// Store into state field - uses the object and the state currently on the stack.
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

			return _targetField.FieldType;
		}
        
        /*
		// Extract an item from a list and return it.
		public override async Task<dynamic> Go(Context context, PageState pageState)
        {
            var listOfItmes = await ReadValue(context, pageState, "listOfItmes");
            dynamic rawIndex = await ReadValue(context, pageState, "index");
            int index = -1;

            if (rawIndex is JValue)
            {
                index = (int)((JValue)rawIndex.ToObject<int>());
            }

            if (index < 0)
            {
                return null;
            }

            if (listOfItmes != null)
            {
                if (listOfItmes is Array)
                {
                    var arrayOfItems = listOfItmes as Array;
                    return index < arrayOfItems.Length ? arrayOfItems.GetValue(index) : null;
                }
                else if (listOfItmes is JArray)
                {
                    var jArray = listOfItmes as JArray;
                    return index < jArray.Count ? jArray[index] : null;
                }
                else
                {
                    try
                    {
                        // Attempt to convert the value back to is json form
                        JArray jArray = JArray.FromObject(listOfItmes);

                        if (jArray != null && index < jArray.Count)
                        {
                            JToken jItem = jArray[index];
                            // Is this conversion necessary?
                            return jItem.ToObject<dynamic>();
                        }
                    }
                    catch (Exception e)
                    {
                        return null;
                    }
                }
            }

            return null;
        }
        */
    }
}
