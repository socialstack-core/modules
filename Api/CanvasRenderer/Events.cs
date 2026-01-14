using System.Collections.Generic;
using Api.CanvasRenderer;

namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{
        /// <summary>
		/// Set of events for the compilation.
		/// </summary>
		public static CompilerEventGroup Compiler;
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

    /// <summary>
	/// Custom user specific events.
	/// </summary>
	public class CompilerEventGroup : EventGroup
	{	
		/// <summary>
		/// Fires before compilation of the UI
		/// </summary>
		public EventHandler<SourceFileContainerSet> BeforeCompile;
		
		/// <summary>
		/// Fires when the file map changed.
		/// </summary>
		public EventHandler<List<UIBundle>> OnMapChange;

		/// <summary>
		/// Fires after compilation of the UI
		/// </summary>
		public EventHandler<SourceFileContainerSet> AfterCompile;
	}

}
