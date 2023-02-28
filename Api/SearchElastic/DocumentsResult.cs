using System.Collections.Generic;

namespace Api.SearchElastic
{
    /// <summary>
    /// Search results with aggregations (facets)
    /// </summary>
    public class DocumentsResult
    {
        public List<Document> Results { get; set; }
        public List<Aggregation> Aggregations { get; set; }
    }

    /// <summary>
    /// Summary of available aggregations/facets
    /// </summary>
    public class Aggregation
    {
        public string Name { get; set; }
        public List<Bucket> Buckets { get; set; }
    }


    /// <summary>
    /// Single aggregation/facet entity
    /// </summary>
    public class Bucket
    {
        public string Key { get; set; }
        public long? Count { get; set; }
    }
}
