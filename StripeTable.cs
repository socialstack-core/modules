using Api.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Api.ContentSync
{
	/// <summary>
	/// Sets of stripes per each table in the DB.
	/// </summary>
	public class StripeTable
	{
		/// <summary>The data tables, indexed by lowercase table name.</summary>
		public Dictionary<string, IdAssigner> DataTables = new Dictionary<string, IdAssigner>();
		
		/// <summary>The raw stripe IDs.</summary>
		public int[] IdTable;

		/// <summary>
		/// Combined maximum stripe index.
		/// </summary>
		public int Max;

		/// <summary>
		/// Creates a new stripe table with the ability to assign IDs from the given ranges.
		/// </summary>
		/// <param name="ranges"></param>
		/// <param name="maxStripeId"></param>
		public StripeTable(List<StripeRange> ranges, int maxStripeId){
			// Expand the ID table:
			IdTable = StripeRange.Expand(ranges);
			Max = maxStripeId;
		}

		/// <summary>
		/// Sets up this stripe table.
		/// </summary>
		/// <param name="database"></param>
		public async Task Setup(DatabaseService database)
		{
			// For each table:
			var schema = database.Schema;

			foreach (var kvp in schema.Tables)
			{
				var tableName = kvp.Key;
				var latestIds = Query.List<LatestStripeId>();
				latestIds.SetRawQuery("SELECT Max(Id) as Id, Id%" + Max + " as StripeId FROM " + tableName + " group by StripeId");

				// Note: this will only be for stripes that are actually in the database.
				// Empty tables for example - this set has 0 entries.
				var latestStripeIds = await database.List(null, latestIds, null);

				var stripeIdLookup = new Dictionary<int, LatestStripeId>();

				foreach (var latestId in latestStripeIds)
				{
					// Special case for == max, as it comes out as a 0 due to the use of mod.
					// Correct it back to being max.
					if (latestId.StripeId == 0)
					{
						latestId.StripeId = Max;
					}

					stripeIdLookup[(int)latestId.StripeId] = latestId;
				}

				// Create the stripe ID set:
				var stripeIds = new LatestStripeId[IdTable.Length];

				for (var i = 0; i < IdTable.Length; i++)
				{
					var stripeId = IdTable[i];

					// Got one in the db?
					if (!stripeIdLookup.TryGetValue(stripeId, out LatestStripeId latestId))
					{
						latestId = new LatestStripeId()
						{
							StripeId = stripeId,
							Id = 0
						};
					}

					stripeIds[i] = latestId;
				}

				// Create an ID assigner for this table:
				DataTables[tableName] = new IdAssigner(stripeIds, Max);
			}
		}
		
	}

	/// <summary>
	/// The latest ID in a particular stripe.
	/// </summary>
	public class LatestStripeId {
		/// <summary>
		/// The latest ID in a particular stripe.
		/// </summary>
		public long Id;
		/// <summary>
		/// The stripe ID that this max ID relates to.
		/// </summary>
		public long StripeId;
	}

	/// <summary>
	/// Assigns IDs for a particular stripe.
	/// </summary>
	public class IdAssigner {

		/// <summary>
		/// Current index in the ID table.
		/// The next ID will be Offset + IdTable[ActiveIndex];
		/// If ActiveIndex tops out, IdTable[IdTable.Length-1] is added to Offset.
		/// </summary>
		public int ActiveIndex;

		/// <summary>
		/// The max ID.
		/// </summary>
		public int Max;

		/// <summary>
		/// Stripes that can be assigned from.
		/// </summary>
		public LatestStripeId[] Stripes;

		/// <summary>
		/// Creates an ID assigner which can assign IDs from the given set of stripes.
		/// </summary>
		/// <param name="stripes"></param>
		/// <param name="max"></param>
		public IdAssigner(LatestStripeId[] stripes, int max)
		{
			Max = max;
			Stripes = stripes;

			// Find the lowest ID - it'll be set as the initial active index:
			var currentMin = stripes[0].Id;
			
			for (var i = 1; i < stripes.Length; i++)
			{
				if (stripes[i].Id < currentMin)
				{
					ActiveIndex = i;
					currentMin = stripes[i].Id;
				}
			}
		}

		/// <summary>
		/// Gets the next ID in the sequence.
		/// </summary>
		/// <returns></returns>
		public long Assign()
		{
			var stripe = Stripes[ActiveIndex++];

			if (ActiveIndex == Stripes.Length)
			{
				// Wrap:
				ActiveIndex = 0;
			}

			if (stripe.Id == 0)
			{
				// The first assignment is just the stripe ID itself:
				stripe.Id = stripe.StripeId;
			}
			else
			{
				// Everything after, we add max to it:
				stripe.Id += Max;
			}
			return stripe.Id;
		}

	}
}