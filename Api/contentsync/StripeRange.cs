using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Api.ContentSync
{
	/// <summary>
	/// The appsettings.json config block for push notification config.
	/// </summary>
    public class StripeRange
    {
		/// <summary>
		/// Numeric server ID.
		/// </summary>
		public int ServerId { get; set; }
		
		/// <summary>
		/// Numeric port.
		/// </summary>
		public int Port { get; set; }

		/// <summary>
		/// When running in production, this is the remote address (DNS/ IP) to connect to in order to sync in realtime.
		/// Updates, inserts and deletes are informed on all cached services.
		/// Note that if this address is unreachable, updates are buffered in memory for a set 
		/// period of time, and connections are repeatedly attempted.
		/// </summary>
		public string RemoteAddress { get; set; }
		
		/// <summary>
		/// Like RemoteAddress, this is used to define which IP "this" machine should bind to.
		/// </summary>
		public string BindAddress { get; set; }

		/// <summary>
		/// Minimum stripe ID (inclusive).
		/// </summary>
		public int Min { get; set; }
		
		/// <summary>
		/// Maximum stripe ID (inclusive).
		/// </summary>
		public int Max { get; set; }

		/// <summary>
		/// You can make multiple ranges overlap by specifying a step size.
		/// </summary>
		public int StepSize { get; set; }


		/// <summary>
		/// Converts a set of ranges into a set of allocatable IDs.
		/// </summary>
		/// <param name="ranges"></param>
		/// <returns></returns>
		public static int[] Expand(List<StripeRange> ranges)
		{
			List<int> idSet = new List<int>();

			foreach (var range in ranges)
			{
				var step = range.StepSize;

				if (step <= 0)
				{
					step = 1;
				}

				for (var i = range.Min; i <= range.Max; i += step)
				{
					idSet.Add(i);
				}
			}

			return idSet.ToArray();
		}
	}
	
}
