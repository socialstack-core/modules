using Api.Contexts;
using System;
using System.Collections.Generic;
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
        ///  A string representing the sort direction.
        /// </summary>
        ///
        public string SortDirection;
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
		public override Task<bool> IsGranted(Capability cap, Context token, object[] extraArgs)
        {
            // TODO: Does this need a perm? It is just sorting after all.
            return Task.FromResult(true);
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

            // Is the direction equal to desc? If not, return it to our default asc value.
            if(direction != "desc")
            {
                direction = "asc";
            }

            Sorts.Add(new FilterSort()
            {
                Field = fieldName,
                SortDirection = direction,
                Type = DefaultType
            });

            return this;
        }
    }
}
