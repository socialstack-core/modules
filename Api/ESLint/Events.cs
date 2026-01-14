

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
        /// All logging events.
        /// </summary>
        public static ESLintEventGroup ESLint = new(); 
    }

    /// <summary>
    /// ESLint event group
    /// </summary>
    public class ESLintEventGroup : EventGroup
    {
        /// <summary>
        /// When a file is changed.
        /// </summary>
        public EventHandler<FilesystemChange> Change;
    }
}