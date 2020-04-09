using Newtonsoft.Json.Linq;
using System.Collections;

namespace Api.Permissions
{

	/// <summary>
	/// A filter applying to a more specific type. This is for convenience - it avoids needing to declare the type for fields.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public partial class Filter<T> : Filter
	{
		/// <summary>
		/// Create a new filter with no restrictions by default.
		/// </summary>
		public Filter() { }

		/// <summary>
		/// Builds a filter safely from a generic JSON payload.
		/// </summary>
		/// <param name="fromRequest"></param>
		public Filter(JObject fromRequest) : base(fromRequest, typeof(T)) { }


		/// <summary>
		/// Copies this filter.
		/// </summary>
		/// <returns></returns>
		public override Filter Copy(bool withNodes = true)
		{
			var filter = new Filter<T>()
			{
				Role = Role
			};
			if (withNodes)
			{
				CopyNodes(filter);
			}
			return filter;
		}
		
		/// <summary>
		/// Adds a new node to the filter.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private Filter<T> Add(FilterNode node)
		{
			Nodes.Add(node);
			return this;
		}

		/// <summary>
		/// Adds a filter node which checks if the value at the given argIndex equals the given value.
		/// If the arg is an object, it should be of the given type. The value will be obtained from the given field.
		/// </summary>
		/// <param name="fieldName"></param>
		/// <param name="value"></param>
		/// <param name="argIndex"></param>
		/// <returns></returns>
		public Filter<T> Equals(string fieldName, object value, int argIndex = 0)
		{
			return Add(new FilterFieldEquals(typeof(T), fieldName)
			{
				Value = value,
				ArgIndex = argIndex
			});
		}

        /// <summary>
        /// Adds a filter node which checks if the value at the given argIndex is less than the given value.
        /// If the arg is an object, it should be of the given type. The value will be obtained from the given field.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <param name="argIndex"></param>
        /// <returns></returns>
        public Filter<T> LessThan(string fieldName, object value, int argIndex = 0)
        {
            return Add(new FilterFieldLessThan(typeof(T), fieldName)
            {
                Value = value,
                ArgIndex = argIndex
            });
        }

        /// <summary>
        /// Adds a filter node which checks if the value at the given argIndex is greater than the given value.
        /// If the arg is an object, it should be of the given type. The value will be obtained from the given field.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <param name="argIndex"></param>
        /// <returns></returns>
        public Filter<T> GreaterThan(string fieldName, object value, int argIndex = 0)
        {
            return Add(new FilterFieldGreaterThan(typeof(T), fieldName)
            {
                Value = value,
                ArgIndex = argIndex
            });
        }

        /// <summary>
        /// Adds a filter node which checks if the value at the given argIndex equals the given value.
        /// If the arg is an object, it should be of the given type. The value will be obtained from the given field.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="argIndex"></param>
        /// <returns></returns>
        public Filter<T> EqualsArg(string fieldName, int argIndex = 0)
		{
			return Add(new FilterFieldEquals(typeof(T), fieldName)
			{
				AlwaysArgMatch = true,
				ArgIndex = argIndex
			});
		}

		/// <summary>
		/// Convenience function for granting a capability only if we're provided a number from the given set.
		/// </summary>
		/// <returns></returns>
		public Filter<T> Id(IEnumerable ids)
		{
			return Add(new FilterFieldEqualsSet(typeof(T), "Id")
			{
				Values = ids
			});
		}

		/// <summary>
		/// Convenience function for granting a capability only if we're provided an entry from the given set.
		/// </summary>
		/// <returns></returns>
		public Filter<T> EqualsSet(string fieldName, IEnumerable values)
		{
			return Add(new FilterFieldEqualsSet(typeof(T), fieldName)
			{
				Values = values
			});
		}

		/// <summary>
		/// Usage: .Thing().And().OtherThing() - this will be true if both are true.
		/// </summary>
		/// <returns></returns>
		public new Filter<T> Or()
		{
			return Add(new FilterOr());
		}

		/// <summary>
		/// Usage: .Thing().And().OtherThing() - this will be true if both are true.
		/// </summary>
		/// <returns></returns>
		public new Filter<T> And()
		{
			return Add(new FilterAnd());
		}

		/// <summary>
		/// Adds a NOT which will invert the result following call.
		/// </summary>
		/// <returns></returns>
		public new Filter<T> Not()
		{
			return Add(new FilterNot());
		}

	}

}