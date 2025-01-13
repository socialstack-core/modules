using Api.Contexts;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Api.CanvasRenderer
{
    /// <summary>
    /// A conditional "if" node.
    /// </summary>
    public class If : Executor
    {
		private FieldBuilder _targetField;
        

        /// <summary>
        /// Create a new conditional "if" node using data in the given JSON token.
        /// </summary>
        /// <param name="d"></param>
		public If(JToken d) : base(d)
        {
        }

		/// <summary>
		/// Emits JSON in to the datamap for an outputted field.
		/// </summary>
		/// <param name="compileEngine"></param>
		/// <param name="field"></param>
		public override void EmitOutputJson(NodeLoader compileEngine, string field)
		{
            // Output a "true" or a "false" depending on what the output value was.
            EmitOutputRead(compileEngine, field);

            var lbl = compileEngine.CodeBody.DefineLabel();
            var endOfBlock = compileEngine.CodeBody.DefineLabel();
            compileEngine.CodeBody.Emit(OpCodes.Brtrue, lbl);
            compileEngine.EmitWriteASCII("false");
			compileEngine.CodeBody.Emit(OpCodes.Br, endOfBlock);
			compileEngine.CodeBody.MarkLabel(lbl);
			compileEngine.EmitWriteASCII("true");
			compileEngine.CodeBody.MarkLabel(endOfBlock);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compileEngine"></param>
        public override ValueTask Compile(NodeLoader compileEngine)
        {
            if (!GetConstString("operandType", out string opType))
            {
                throw new NotImplementedException("Runtime operand types are not supported by the If node in server graphexec.");
            }

            // Load the state for the stfld later:
            compileEngine.EmitLoadState();

            // A or B can be not present (null or 0, depending on what it is we are comparing).
            Type inputTypeA;
            Type inputTypeB;
            bool isANumeric = false;
            bool isBNumeric = false;

            if (GetConstNumber("operanda", out long opAL))
            {
                inputTypeA = typeof(long);
                compileEngine.CodeBody.Emit(OpCodes.Ldc_I8, opAL);
                isANumeric = true;
            }
            else if (GetConstString("operanda", out string opA) && opA != null)
            {
                inputTypeA = typeof(string);
                compileEngine.CodeBody.Emit(OpCodes.Ldstr, opA);
            }
            else
            {
                inputTypeA = compileEngine.EmitLoadInput("operanda", this, true);
                if (inputTypeA == typeof(uint) || inputTypeA == typeof(int) || inputTypeA == typeof(uint?) || inputTypeA == typeof(int?) || inputTypeA == typeof(ulong) || inputTypeA == typeof(long) || inputTypeA == typeof(ulong?) || inputTypeA == typeof(long?))
                {
                    isANumeric = true;
                }
            }

            if (GetConstNumber("operandb", out long opBL))
            {
                inputTypeB = typeof(long);
                compileEngine.CodeBody.Emit(OpCodes.Ldc_I8, opBL);
                isBNumeric = true;
            }
            else if (GetConstString("operandb", out string opB) && opB != null)
			{
				inputTypeB = typeof(string);
                compileEngine.CodeBody.Emit(OpCodes.Ldstr, opB);
            }
            else
            {
                inputTypeB = compileEngine.EmitLoadInput("operandb", this, true);
                if (inputTypeB == typeof(uint) || inputTypeB == typeof(int) || inputTypeB == typeof(uint?) || inputTypeB == typeof(int?) || inputTypeB == typeof(ulong) || inputTypeB == typeof(long) || inputTypeB == typeof(ulong?) || inputTypeB == typeof(long?))
                {
                    isBNumeric = true;
                }
            } 

            if (inputTypeA == null && inputTypeB == null)
            {
                throw new InvalidOperationException("If node has no inputs");
            }

            if (inputTypeA!= null && inputTypeB != null && inputTypeA != inputTypeB && (inputTypeA.IsValueType || inputTypeB.IsValueType) && !(isANumeric && isBNumeric))
            {
                throw new NotImplementedException("Can't compare different types with an If node yet.");
            }

            // If either is null, then put a default of the other type on the stack.
            Type toUse = null;

            if (inputTypeA == null)
            {
                toUse = inputTypeB;
            }
            else if (inputTypeB == null)
            {
                toUse = inputTypeA;
            }

            if (toUse != null)
            {
                if (toUse == typeof(string) || !toUse.IsValueType)
                {
                    compileEngine.CodeBody.Emit(OpCodes.Ldnull);
                }
                else
                {
					// A zero
					compileEngine.CodeBody.Emit(OpCodes.Ldc_I4, 0);
				}
            }

            switch (opType)
            {
                case "lessThan":
                    compileEngine.CodeBody.Emit(OpCodes.Clt);
                    break;
                case "moreThan":
                    compileEngine.CodeBody.Emit(OpCodes.Cgt);
                    break;
                case "lessThanEqual":
                    compileEngine.CodeBody.Emit(OpCodes.Cgt);
                    compileEngine.CodeBody.Emit(OpCodes.Not);
                    break;
                case "moreThanEqual":
                    compileEngine.CodeBody.Emit(OpCodes.Clt);
                    compileEngine.CodeBody.Emit(OpCodes.Not);
                    break;
                case "equalTo":
                    // If it is strings, we need to use string.Equals
                    if (toUse == null && inputTypeA == typeof(string) && inputTypeB == typeof(string))
                    {
                        // Both string types.
                        var equals = typeof(string).GetMethod(
                            "Equals",
                            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                            new Type[] { typeof(string), typeof(string) }
                        );

						compileEngine.CodeBody.Emit(OpCodes.Call, equals);
					}
                    else
                    {
						compileEngine.CodeBody.Emit(OpCodes.Ceq);
					}
                break;
			}

			_targetField = compileEngine.DefineStateField(typeof(bool));

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
		public override async Task<dynamic> Go(Context context, PageState pageState)
        {
            dynamic operanda = await ReadValue(context, pageState, "operanda");
            dynamic operandb = await ReadValue(context, pageState, "operandb");
            string operandType = await ReadValue(context, pageState, "operandType") as string;

            if (operandType == null)
            {
                return false;
            }

            try
            {
                switch (operandType)
                {
                    case "lessThan":
                        return operanda < operandb;

                    case "moreThan":
                        return operanda > operandb;

                    case "lessThanEqual":
                        return operanda <= operandb;

                    case "moreThanEqual":
                        return operanda >= operandb;

                    case "equalTo":
                        return operanda == operandb;

                    default:
                        return false;
                }
            } 
            catch (Exception e)
            {
                // Lazy implementation - if types are incomparable then catch error and return false
                return false;
            }
        }
        */
	}
}
