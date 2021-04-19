using Api.Contexts;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Api.Permissions
{
    /// <summary>
    /// A filter method which is active if there is a sort.
    /// </summary>
    public partial class FilterSort : FilterNode
    {
        /// <summary>
		/// The type we're sorting.
		/// </summary>
		public Type Type;

        /// <summary>
        /// The field that we are sorting by.
        /// </summary>
        public string Field;

        /// <summary>
        /// The field being sorted. Can be null as this is lazy loaded.
        /// </summary>
        public FieldInfo FieldInfo;

        /// <summary>
        /// True if this is sorting asc.
        /// </summary>
        public bool Ascending;
        /*
        public enum SortDirection
        {
            /// <summary>
            /// Sort this in Ascending order.
            /// </summary>
            Asc,
            /// <summary>
            /// Sort this in descending order
            /// </summary>
            Desc
        } */

        /// <summary>
		/// True if this particular node is granted.
		/// </summary>
		public override ValueTask<bool> IsGranted(Capability cap, Context token, object firstArg)
        {
            return new ValueTask<bool>(true);
        }
		
    }

    public partial class Filter
    {
        /// <summary>
        /// Sort this filter by the given field name from the filters default type.
        /// </summary>
        public Filter Sort(string fieldName, string direction = "asc")
        {
            if (Sorts == null)
            {
                Sorts = new List<FilterSort>();
            }

            Sorts.Add(new FilterSort()
            {
                Field = fieldName,
                Ascending = (direction != "desc"),
                Type = DefaultType
            });

            return this;
        }
    }
}
