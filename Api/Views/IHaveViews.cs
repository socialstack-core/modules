using Api.Permissions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Views
{
	/// <summary>
	/// Implement this interface on a type to automatically add reaction support.
	/// If you're extending a built in type, it's best to put the extension in its own module under Api/ViewTypes/{TypeName}.
	/// </summary>
	public partial interface IHaveViews
    {
		/// <summary>
		/// The view counter. You MUST declare a field called TotalViews which this updates.
		/// </summary>
		int Views { get; set; }
	}
}
