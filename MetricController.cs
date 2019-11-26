using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Contexts;
using Api.Results;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Metrics
{
    /// <summary>
    ///  Handles Metric endpoints.
    /// </summary>
    [Route("v1/metric")]
    [ApiController]
    public partial class MetricController: ControllerBase
    {
        private IMetricService _metrics;

        /// <summary>
        /// Instanced automatically.
        /// </summary>
        public MetricController(IMetricService metrics)
        {
            _metrics = metrics;
        }

        /// <summary>
        /// Get /v1/metric/2/
        /// Returns the metrics data for a single metric.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<Metric> Load([FromRoute] int id)
        {
            var context = Request.GetContext();
            var result = await _metrics.Get(context, id);
            return await Events.MetricLoad.Dispatch(context, result, Response);
        }

        /// <summary>
        /// Delete /v1/metric/2
        /// Deletes a metric.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<Metric> Delete([FromRoute] int id)
        {
            var context = Request.GetContext();
            var result = await _metrics.Get(context, id);
            result = await Events.MetricDelete.Dispatch(context, result, Response);

            if (result == null || !await _metrics.Delete(context, id))
            {
                // The handlers have blocked this one from happening, or it failed
                return null;
            }

            return result;
        }

        /// <summary>
        /// Get /v1/metric/list
        /// Lists all metrics available to this user
        /// </summary>
        /// <returns></returns>
        [HttpGet("list")]
        public async Task<Set<Metric>> List()
        {
            return await List(null);
        }

        /// <summary>
        /// POST /v1/metric/list
        /// Lists filtered metrics available to this user.
        /// See the filter documentation for more details on what you can request here.
        /// </summary>
        /// <returns></returns>
        [HttpPost("list")]
        public async Task<Set<Metric>> List([FromBody] JObject filters)
        {
            var context = Request.GetContext();
            var filter = new Filter<Metric>(filters);

            filter = await Events.MetricList.Dispatch(context, filter, Response);

            if(filter == null)
            {
                // A handler rejected this request
                return null;
            }

            var results = await _metrics.List(context, filter);
            return new Set<Metric>() { Results = results };
        }

        /// <summary>
        /// Post /v1/metric/
        /// Creates a new metric. Returns the ID.
        /// </summary>
        [HttpPost]
        public async Task<Metric> Create([FromBody] MetricAutoForm form)
        {
            var context = Request.GetContext();

            // Start building up our object.
            // Most other fields, particularly custom extensions, are handled by autofrom.
            var metric = new Metric();

            if (!ModelState.Setup(form, metric))
            {
                return null;
            }

            form = await Events.MetricCreate.Dispatch(context, form, Response);

            if (form == null || form.Result == null)
            {
                // A handler rejected this request.
                return null;
            }

            metric = await _metrics.Create(context, form.Result);

            if (metric == null)
            {
                Response.StatusCode = 500;
                return null;
            }
            return metric;
        }

        /// <summary>
        /// POST /v1/metric/1
        /// Updates a metric with the given ID
        /// </summary>
        /*[HttpPost("{id}")]
        public async Task<Metric> Update([FromRoute] int id, [FromBody] MetricAutoForm form)
        {
            var context = Request.GetContext();

            var metric = await _metrics.Get(context, id);

            if (!ModelState.Setup(form, metric))
            {
                return null;
            }

            form = await Events.MetricUpdate.Dispatch(context, form, Response);

            if (form == null || form.Result == null)
            {
                // A handler rejected this request.
                return null;
            }

            metric = await _metrics.Update(context, form.Result);

            if(metric == null)
            {
                Response.StatusCode = 500;
                return null;
            }
            return metric;
        }*/
    }
}
