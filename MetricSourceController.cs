using Microsoft.AspNetCore.Mvc;


namespace Api.Metrics
{
    /// <summary>
    ///  Handles Metric source endpoints.
    /// </summary>
    [Route("v1/metricsource")]
    public partial class MetricSourceController: AutoController<MetricSource>
    {
    }
}
