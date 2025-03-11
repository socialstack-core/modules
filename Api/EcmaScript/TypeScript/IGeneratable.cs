

namespace Api.EcmaScript.TypeScript
{
    /// <summary>
    /// The most primitive thing in the TypeScript module
    /// </summary>
    public interface IGeneratable
    {
        /// <summary>
        /// Takes relevant information and outputs TypeScript Source Code.
        /// </summary>
        /// <returns></returns>
        public string CreateSource();
    }
}