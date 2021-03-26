using System;
using Api.AutoForms;

namespace Api.Users
{
    /// <summary>
    /// Implement this interface on a type to add automatic CreatedUtc and EditedUtc support.
    /// </summary>
    public partial interface IHaveTimestamps
    {
		/// <summary>
		/// The UTC creation date.
		/// </summary>
		DateTime GetCreatedUtc();

		/// <summary>
		/// The UTC last edited date.
		/// </summary>
		DateTime GetEditedUtc();


	}
}
