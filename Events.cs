using Api.Permissions;
using System.Collections.Generic;

namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
		/// <summary>
		/// Triggered after the underlying frontend JS has changed.
		/// Triggers most often on development environments; often only once on startup for prod.
		/// </summary>
		public static EventHandler<long> FrontendjsAfterUpdate;

		/// <summary>
		/// Triggered after the underlying frontend CSS has changed.
		/// Triggers most often on development environments; often only once on startup for prod.
		/// </summary>
		public static EventHandler<long> FrontendCssAfterUpdate;

		/// <summary>
		/// Triggers whenever the frontend changed at all.
		/// </summary>
		public static EventHandler<long> FrontendAfterUpdate;

	}

}
