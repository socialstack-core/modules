
using System;
using System.Collections.Generic;
using System.Text;
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
        /// Custom event handlers to handle typescript related events. 
        /// </summary>
        public static TypeScriptGroup TypeScript;
    }
    
    /// <summary>
    /// A collection of events that allows
    /// a subscriber to append TS Config paths
    /// or when the Api Container is available.
    /// </summary>
    public class TypeScriptGroup : EventGroup
    {
        /// <summary>
        /// Use this to add custom records into the TSConfig > Compiler Options > Paths.
        /// </summary>
        public EventHandler<StringBuilder> TSConfigPaths;
        
        /// <summary>
        /// Dispatches the ApiContainer so subscribers can push custom TS files in there. 
        /// </summary>
        public EventHandler<SourceFileContainer> ApiContainer;
    }
}