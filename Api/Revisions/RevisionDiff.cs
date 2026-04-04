using System;
using Api.Database;

namespace Api.Revisions
{
    /// <summary>
    /// Represents the difference between two versions of an object in a revision history.
    /// </summary>
    /// <remarks>
    /// This is the base class for revision tracking. Derived classes implement type-safe
    /// storage and retrieval of previous and next chronological states. Use the generic
    /// <see cref="RevisionDiff{T, ID}"/> class for strongly-typed revision comparisons.
    /// </remarks>
    public partial class RevisionDiff
    {
        /// <summary>
        /// Gets the previous chronological state in the revision history.
        /// </summary>
        /// <returns>
        /// An <see cref="object"/> representing the previous version of the tracked content.
        /// Derived implementations return instances of the specific content type.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// Thrown when called on the base class. Override in derived classes to provide implementation.
        /// </exception>
        /// <remarks>
        /// This method is abstract in intent and must be overridden by derived classes to provide
        /// access to the antecedent state. The base implementation throws <see cref="NotImplementedException"/>.
        /// </remarks>
        public virtual object GetPreviousObject()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the next chronological state in the revision history.
        /// </summary>
        /// <returns>
        /// An <see cref="object"/> representing the next version of the tracked content.
        /// Derived implementations return instances of the specific content type.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// Thrown when called on the base class. Override in derived classes to provide implementation.
        /// </exception>
        /// <remarks>
        /// This method is abstract in intent and must be overridden by derived classes to provide
        /// access to the subsequent state. The base implementation throws <see cref="NotImplementedException"/>.
        /// </remarks>
        public virtual object GetNextObject()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// A strongly-typed wrapper for tracking revisions between two chronologically sequential versions of content.
    /// </summary>
    /// <typeparam name="T">
    /// The content type being tracked. Must derive from <see cref="Content{ID}"/> and provide a parameterless constructor.
    /// </typeparam>
    /// <typeparam name="ID">
    /// The type of the identifier used by <typeparamref name="T"/>.
    /// Must be a value type implementing <see cref="IConvertible"/>, <see cref="IEquatable{ID}"/>, and <see cref="IComparable{ID}"/>.
    /// </typeparam>
    /// <remarks>
    /// This generic class provides type-safe access to revision history by storing strongly-typed references
    /// to the previous and next states of content. It is useful for audit trails, change tracking, and
    /// version control scenarios. The content objects are typically retrieved from a revision history repository
    /// or database.
    /// </remarks>
    /// <example>
    /// <code>
    /// var article1 = new Article { Id = 1, Title = "First Version" };
    /// var article2 = new Article { Id = 1, Title = "Second Version" };
    /// var diff = new RevisionDiff&lt;Article, int&gt;(article1, article2);
    /// 
    /// var previous = diff.GetPreviousObject(); // Returns article1 as object
    /// var next = diff.GetNextObject();          // Returns article2 as object
    /// </code>
    /// </example>
    public partial class RevisionDiff<T, ID> : RevisionDiff
        where T : Content<ID>, new()
        where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
    {
        /// <summary>
        /// Gets the previous chronological version of the content.
        /// </summary>
        /// <remarks>
        /// This field holds a reference to the antecedent state of the tracked content.
        /// It may be null if this revision represents the first version in the history.
        /// </remarks>
        public T Previous;

        /// <summary>
        /// Gets the next chronological version of the content.
        /// </summary>
        /// <remarks>
        /// This field holds a reference to the subsequent state of the tracked content.
        /// It may be null if this revision represents the latest version in the history.
        /// </remarks>
        public T Next;

        /// <summary>
        /// Initializes a new instance of the <see cref="RevisionDiff{T, ID}"/> class with the specified previous and next content states.
        /// </summary>
        /// <param name="prev">The previous chronological version of the content. May be null.</param>
        /// <param name="next">The next chronological version of the content. May be null.</param>
        /// <remarks>
        /// This constructor pairs two chronologically sequential versions of content into a single revision diff object.
        /// Both parameters are stored as-is without validation or modification.
        /// </remarks>
        public RevisionDiff(T prev, T next)
        {
            Previous = prev;
            Next = next;
        }

        /// <summary>
        /// Gets the previous chronological state in the revision history.
        /// </summary>
        /// <returns>
        /// The <see cref="Previous"/> content as an <see cref="object"/>.
        /// </returns>
        /// <remarks>
        /// This override provides polymorphic access to the previous content while maintaining
        /// the strongly-typed <see cref="Previous"/> field for direct access.
        /// </remarks>
        public override object GetPreviousObject()
        {
            return Previous;
        }

        /// <summary>
        /// Gets the next chronological state in the revision history.
        /// </summary>
        /// <returns>
        /// The <see cref="Next"/> content as an <see cref="object"/>.
        /// </returns>
        /// <remarks>
        /// This override provides polymorphic access to the next content while maintaining
        /// the strongly-typed <see cref="Next"/> field for direct access.
        /// </remarks>
        public override object GetNextObject()
        {
            return Next;
        }
    }
}