
namespace Api.Payments
{
    /// <summary>
    /// Wrapper class for product so that we can expose a unqiue full slug based on its position in the category structure
    /// </summary>
    public struct ProductNode
    {
        /// <summary>
        /// The undelrying product entity
        /// </summary>
        public Product Product;
    }
}
