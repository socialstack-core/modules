using Microsoft.AspNetCore.Mvc;


namespace Api.Metrics
{
    /// <summary>
    ///  Handles Metric endpoints.
    /// </summary>
    [Route("v1/metric")]
    public partial class MetricController: AutoController<Metric>
    {
    }
}
