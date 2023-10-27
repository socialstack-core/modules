namespace Api.CanvasRenderer
{
    /// <summary>
    /// A link that can be found in a graph
    /// </summary>
    public class Link
    {
        /// <summary>
        /// The index of the node in the graph this links to
        /// </summary>
        public int n;

        /// <summary>
        /// The field in the node this links to
        /// </summary>
        public string f;
    }
}
