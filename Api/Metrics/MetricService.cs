﻿using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Diagnostics.Eventing.Reader;
using Api.Eventing;
using System.Timers;
using Api.Contexts;

namespace Api.Metrics
{
    /// <summary>
    /// Handles Metrics.
    /// Instanced automatically. Use Injection to use this service, or Startup.Services.Get. 
    /// </summary>
    public partial class MetricService : AutoService<Metric>
    {
		/// <summary>
		/// The raw metric sample rate is in blocks of every 15 minutes. This is the smallest division available.
		/// </summary>
		private int blockSizeInMinutes = 15;
		private readonly MetricSourceService _sources;
		private readonly MetricMeasurementService _measurements;
		private readonly List<LiveMetricSource> _liveSources = new List<LiveMetricSource>();
		private DateTime epoch = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public MetricService(MetricSourceService sources, MetricMeasurementService measurements) : base(Events.Metric)
		{
			_sources = sources;
			_measurements = measurements;

			InstallAdminPages("Metrics", "fa:fa-chart-bar", new string[] { "id", "name" });

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
			var metrics = await Where(DataOptions.IgnorePermissions).ListAll(ctx);
			var metricSources = await _sources.Where(DataOptions.IgnorePermissions).ListAll(ctx);

			// Map each source to the metrics they originate from.
			// Whilst they'll probably be rare, we want to avoid connecting sources that aren't actually in metrics.
			// First, make a little lookup:
			var metricLookup = new Dictionary<uint, Metric>();

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

				var evtName = source.EventName.ToLower().Trim();

				/*
				if (!Events.All.TryGetValue(evtName, out Eventing.EventHandler handler))
				{
					Console.WriteLine("[WARN] Metric source (see the metricsource table in the database) " + source.Id + " is trying to connect to an event called '" + source.EventName + "' which doesn't exist in this API. " +
						"The source has been ignored.");
					continue;
				}
				*/

				// A connected metric source is a "Live source" - that's one which is actively receiving triggers:
				var liveSource = new LiveMetricSource()
				{
					Metric = metric,
					Source = source
				};

				//TODO:disbaled 
				/*
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
				*/

				_liveSources.Add(liveSource);
			}

		}

		private void StartUpdateLoop()
		{
			// Create a timer with a 30 second interval.
			var metricTimer = new System.Timers.Timer(30 * 1000);

			var countField = _measurements.GetChangeField("Count");

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

					// When there are more than 1024 sources, this optimisation will overflow.
					if (liveSource.Source.Id >= 1024)
					{
						throw new Exception("Too many metric sources - it's currently limited to 1024 in order to operate at high efficiency.");
					}
					uint measurementId = (uint)(unixtime / (blockSizeInMinutes * 60)) | (liveSource.Source.Id << 21);

					// Let's check to see if it exists.
					var measurement = await _measurements.Get(context, measurementId, DataOptions.IgnorePermissions);

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
						if(await _measurements.StartUpdate(context, measurement, DataOptions.IgnorePermissions)){
							measurement.Count += count;
							measurement.MarkChanged(countField);
							await _measurements.FinishUpdate(context, measurement);
						}
					}

				}

			};

			metricTimer.AutoReset = true;
			metricTimer.Enabled = true;
		}
		
    }
}
