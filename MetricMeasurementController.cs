using Microsoft.AspNetCore.Mvc;


namespace Api.Metrics
{
    /// <summary>
    ///  Handles Metric measurement endpoints.
    /// </summary>
    [Route("v1/metricmeasurement")]
    public partial class MetricMeasurementController: AutoController<MetricMeasurement>
    {
    }
}
