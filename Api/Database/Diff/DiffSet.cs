using System.Collections.Generic;


namespace Api.Database
{
    /// <summary>
    /// Represents a set of changes.
    /// </summary>
    public class DiffSet<T, U>
    {
        /// <summary>
        /// A list of things that were added.
        /// </summary>
        public List<T> Added = new List<T>();

        /// <summary>
        /// A list of things that were changed.
        /// </summary>
        public List<U> Changed = new List<U>();

		/// <summary>
		/// A list of things that were removed.
		/// </summary>
		public List<T> Removed = new List<T>();
	}

}
