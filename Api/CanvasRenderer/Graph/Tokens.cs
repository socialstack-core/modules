using Api.Database;
using Api.SocketServerLibrary;
using Api.Startup;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Api.CanvasRenderer
{
    /// <summary>
    /// </summary>
    public class Tokens : Executor
    {
        /// <summary>
        /// A writer field into which the token value is written
        /// </summary>
        public FieldBuilder _outputFld;

        /// <summary>
        /// Token name
        /// </summary>
        public string _sourceText;

        /// <summary>
        /// Create a new token executor.
        /// </summary>
        /// <param name="d"></param>
        public Tokens(JToken d) : base(d)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compileEngine"></param>
        public override ValueTask Compile(NodeLoader compileEngine)
		{
            // Loads this graph node (used by the stfld call)
			compileEngine.EmitLoadState();

            GetConstString("text", out _sourceText);

            if (string.IsNullOrEmpty(_sourceText))
            {
                _sourceText = "";
			}

            // Ld initial string on to the stack.
            compileEngine.CodeBody.Emit(System.Reflection.Emit.OpCodes.Ldstr, _sourceText);

            // Get all the vars present in it:
			var matches = Regex.Matches(_sourceText, @"\$\{(\w|\.)+\}");

            var replaceMethod = typeof(string).GetMethod("Replace", new Type[] { typeof(string), typeof(string) });

			foreach (Match match in matches)
			{
				string variable = match.Value.Substring(2, match.Value.Length - 3); // Remove ${ and }

                if (variable.StartsWith("content."))
                {
                    variable = variable.Substring(8);
                }

				compileEngine.CodeBody.Emit(System.Reflection.Emit.OpCodes.Ldstr, match.Value);

                if (variable.StartsWith("url."))
                {
                    // Load a URL token.
                    var urlTokenName = variable.Substring(4);
                    compileEngine.EmitLoadUrlToken(urlTokenName);

                    // The token value or an empty string is currently on the stack.
                }
                else
                {
                    var inputType = compileEngine.EmitLoadInput(variable, this, true);

                    if (inputType == null)
                    {
                        compileEngine.CodeBody.Emit(System.Reflection.Emit.OpCodes.Ldstr, "{Invalid or otherwise disconnected token value}");
                    }
                    else if (inputType != typeof(string))
                    {
                        var ts = inputType.GetMethod("ToString");
                        compileEngine.CodeBody.Emit(OpCodes.Call, ts);
                    }
                }
				// a=a.Replace(match.Value, stack_value);
				compileEngine.CodeBody.Emit(OpCodes.Call, replaceMethod);
			}

			_outputFld = compileEngine.DefineStateField(typeof(string));
            compileEngine.CodeBody.Emit(OpCodes.Stfld, _outputFld);

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
			compileEngine.EmitLoadState();
			compileEngine.CodeBody.Emit(OpCodes.Ldfld, _outputFld);
			return _outputFld.FieldType;
        }

        /// <summary>
        /// Emits JSON in to the datamap for an outputted field.
        /// </summary>
        /// <param name="compileEngine"></param>
        /// <param name="field"></param>
        public override void EmitOutputJson(NodeLoader compileEngine, string field)
        {
			// Writer to write to:
			compileEngine.EmitWriter();

			// Note that this writer is released at the end.
			compileEngine.EmitLoadState();
            compileEngine.CodeBody.Emit(OpCodes.Ldfld, _outputFld);

			var escaped = typeof(Writer).GetMethod(
				"WriteEscaped",
				new Type[] { typeof(string) }
			);

            compileEngine.CodeBody.Emit(OpCodes.Call, escaped);
		}
	}
}
