

namespace Api.EcmaScript.TypeScript
{
    public partial class ClassMethodArgument : IGeneratable
    {
        public string Name; 

        public string Type;

        public string DefaultValue;

        public string CreateSource()
        {
            var src = $"{Name}: {Type}";

            if (!string.IsNullOrEmpty(DefaultValue))
            {
                if (Type == "string")
                {
                    src += $" = '{DefaultValue.Replace("'", "\\'")}'";
                }
                else
                {
                    src += $" = {DefaultValue}";
                }
            }
            return src;
        }
    }
}