
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
        public static TypeScriptEvents TypeScript = new();
    }
    
    /// <summary>
    /// A collection of events that allows
    /// a subscriber to append TS Config paths
    /// or when the Api Container is available.
    /// </summary>
    public class TypeScriptEvents
    {
        /// <summary>
        /// Use this to add custom records into the TSConfig > Compiler Options > Paths.
        /// </summary>
        public readonly ObjectEvent<StringBuilder> TSConfigPaths = new();
        
        /// <summary>
        /// Dispatches the ApiContainer so subscribers can push custom TS files in there. 
        /// </summary>
        public readonly ObjectEvent<SourceFileContainer> ApiContainer = new();
    }
    
    /// <summary>
    /// An ObjectEvent base class that can be used around all the typescript functionality. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObjectEvent<T>
    {
        /// <summary>
        /// Subscriber collection
        /// </summary>
        private readonly List<Func<T, T>> Handlers = [];
        
        /// <summary>
        /// Add a subscriber to an event group
        /// </summary>
        /// <param name="handler"></param>
        public void AddEventListener(Func<T, T> handler)
        {
            Handlers.Add(handler);
        }
        
        /// <summary>
        /// Dispatch the event, this should only be used
        /// internally. 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public T Dispatch(T item)
        {
            foreach (var handler in Handlers)
            {
                item = handler(item);
            }

            return item;
        }
    }
}