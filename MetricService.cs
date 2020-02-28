using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Api.Eventing;
using System.Timers;
using Api.Contexts;

namespace Api.Metrics
{
    /// <summary>
    /// Handles Metrics.
    /// Instanced automatically. Use Injection to use this service, or Startup.Services.Get. 
    /// </summary>
    public partial class MetricService : AutoService<Metric>, IMetricService
    {
		/// <summary>
		/// The raw metric sample rate is in blocks of every 15 minutes. This is the smallest division available.
		/// </summary>
		private int blockSizeInMinutes = 15;
		private readonly IMetricSourceService _sources;
		private readonly IMetricMeasurementService _measurements;
		private readonly List<LiveMetricSource> _liveSources = new List<LiveMetricSource>();
		private DateTime epoch = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public MetricService(IMetricSourceService sources, IMetricMeasurementService measurements) : base(Events.Metric)
		{
			_sources = sources;
			_measurements = measurements;

			StartUpdateLoop();

			Task.Run(async () => {

				await SetupMetricHandlers();

			});
		}

		/// <summary>
		/// Sets up all the event listeners for initially configured metrics.
		/// </summary>
		/// <returns></returns>
		private async Task SetupMetricHandlers()
		{
			var ctx = new Context();

			// Get the list of all metrics and sources:
			var metrics = await List(ctx, null);
			var metricSources = await _sources.List(ctx, null);

			// Map each source to the metrics they originate from.
			// Whilst they'll probably be rare, we want to avoid connecting sources that aren't actually in metrics.
			// First, make a little lookup:
			var metricLookup = new Dictionary<int, Metric>();

			foreach (var metric in metrics)
			{
				metricLookup[metric.Id] = metric;
			}

			// Connect each source:
			foreach (var source in metricSources)
			{
				if (!metricLookup.TryGetValue(source.MetricId, out Metric metric))
				{
					continue;
				}

				if (!Events.All.TryGetValue(source.EventName.ToLower().Trim(), out Eventing.EventHandler handler))
				{
					Console.WriteLine("[WARN] Metric source (see the metricsource table in the database) " + source.Id + " is trying to connect to an event called '" + source.EventName + "' which doesn't exist in this API. " +
						"The source has been ignored.");
					continue;
				}
				
				// A connected metric source is a "Live source" - that's one which is actively receiving triggers:
				var liveSource = new LiveMetricSource()
				{
					Metric = metric,
					Source = source
				};

				GenericEventHandler listener = (Context context, object[] args) => {

					if (args == null || args.Length == 0)
					{
						return null;
					}

					// The event has triggered. Bump the live source active count:
					liveSource.Count++;

					return args[0];
				};

				handler.AddEventListener(listener);
				
				_liveSources.Add(liveSource);
			}

		}

		private void StartUpdateLoop()
		{
			// Create a timer with a 30 second interval.
			var metricTimer = new System.Timers.Timer(30 * 1000);

			metricTimer.Elapsed += async (Object source, ElapsedEventArgs e) => {

				// Timer tick. Let's store any updated metric measurements.
				var context = new Context();
				
				foreach (var liveSource in _liveSources)
				{
					// If its count is non-zero, add to or update the database entry.
					if (liveSource.Count == 0)
					{
						continue;
					}

					var count = liveSource.Count;
					liveSource.Count = 0;

					// Got a metric measurement value to add:
					long unixtime = (long)(DateTime.UtcNow.Subtract(epoch)).TotalSeconds;

					#warning At some point, somebody will create more than 1024 sources.
					// When they do, this optimisation will overflow.
					int measurementId = (int)(unixtime / (blockSizeInMinutes * 60)) | (liveSource.Source.Id << 21);

					// Let's check to see if it exists.
					var measurement = await _measurements.Get(context, measurementId);

					if (measurement == null)
					{
						// Doesn't exist, create it.
						await _measurements.Create(context, new MetricMeasurement() {
							Id = measurementId,
							Count = count,
							MetricId = liveSource.Metric.Id,
							SourceId = liveSource.Source.Id
						});
					}
					else
					{
						measurement.Count += count;
						await _measurements.Update(context, measurement);
					}

				}

			};

			metricTimer.AutoReset = true;
			metricTimer.Enabled = true;
		}
		
    }
}
