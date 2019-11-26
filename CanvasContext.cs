using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Api.CanvasRenderer
{
	/// <summary>
	/// Holds context values for a canvas being rendered.
	/// If the template is an email, for example, then this holds e.g. a person's name.
	/// It acts like data posted to the page.
	/// </summary>
	public class CanvasContext
    {
        /// <summary>
        /// The contents of the context.
        /// </summary>
        public Dictionary<string, object> Context = new Dictionary<string, object>();
		
		/// <summary>
		/// Copy this context into the given one.
		/// </summary>
		/// <param name="target"></param>
        public void CloneInto(CanvasContext target)
        {
            foreach (var kvp in Context)
            {
                target.Context[kvp.Key] = kvp.Value;
            }
        }

		/// <summary>
		/// Try to get a value from the context.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <returns></returns>
        public bool TryGetValue(string name, out object value)
        {
            return Context.TryGetValue(name, out value);
        }

		/// <summary>
		/// Gets a value from the context.
		/// </summary>
		/// <param name="name"></param>
		/// <returns>Null if it doesn't exist.</returns>
        public object this[string name]
        {
            get
            {
                Context.TryGetValue(name, out object result);
                return result;
            }
            set
            {
                Context[name] = value;
            }
        }
    }
}
