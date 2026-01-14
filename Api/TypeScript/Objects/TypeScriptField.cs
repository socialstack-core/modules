using System;
using System.Text;

namespace Api.TypeScript.Objects
{
    /// <summary>
    /// A field definition in TS
    /// </summary>
    public class TypeScriptField : AbstractTypeScriptObject
    {
        /// <summary>
        /// Name of the field
        /// </summary>
        public string FieldName { get; set; }
    
        /// <summary>
        /// The fields type
        /// </summary>
        public Type FieldType { get; set; }
    
        /// <summary>
        /// Write out as typescript source
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="svc"></param>
        public override void ToSource(StringBuilder builder, TypeScriptService svc)
        {
            builder.AppendLine(
            $"    {TypeScriptService.LcFirst(FieldName)}{(TypeScriptService.IsNullable(FieldType) ? "?:" : ":")} {svc.GetGenericSignature(FieldType)}"
            );
        }
    }
}