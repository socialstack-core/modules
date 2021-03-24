using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using System;
using System.Text;
using System.Linq;

namespace Api.Huddles
{
	/// <summary>
	/// Handles huddleServers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class HuddleServerService : AutoService<HuddleServer>
    {
		/// <summary>
		/// The size in seconds of a time slice (15 minutes).
		/// </summary>
		private const int TimeSliceSize = 60 * 15;
		
		/// <summary>
		/// Time slice epoch.
		/// </summary>
		private DateTime _epoch = new DateTime(2020, 1, 1);
		
		private string queryStart;
		
		private HuddleServer[] huddleServerSet;
		private Dictionary<int, HuddleServer> huddleServerLookup;

		private Random rand = new Random();

		private HuddleLoadMetricService _loadMetrics;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public HuddleServerService(HuddleLoadMetricService loadMetrics) : base(Events.HuddleServer)
        {
			_loadMetrics = loadMetrics;

			InstallAdminPages("Huddle Servers", "fa:fa-users", new string[] { "id", "address" });
			
			Cache(new CacheConfig<HuddleServer>(){
				Retain = true,
				Preload = true,
				OnCacheLoaded = () => {
					// The cache ID index is a huddle server lookup.
					// That'll be useful when allocating a server.
					huddleServerLookup = GetCacheForLocale(1).GetPrimary();
					huddleServerSet = huddleServerLookup.Values.ToArray();
				}
			});
			
			queryStart = "select HuddleServerId from " + typeof(HuddleLoadMetric).TableName() + 
				" where TimeSliceId in (";
		}

		private string[] _hostList;

		/// <summary>
		/// Gets the list of server addresses.
		/// </summary>
		/// <returns></returns>
		public string[] GetHostList()
		{
			if (_hostList == null)
			{
				if (huddleServerSet == null)
				{
					return null;
				}
				_hostList = new string[huddleServerSet.Length];
				for (var i = 0; i < huddleServerSet.Length; i++)
				{
					_hostList[i] = huddleServerSet[i].Address;
				}
			}

			return _hostList;
		}

		private int _currentRand = 0;
		
		/// <summary>
		/// The next random server.
		/// </summary>
		public HuddleServer RandomServer(){
			if(huddleServerSet == null || huddleServerSet.Length == 0){
				return null;
			}
			
			if(_currentRand >= huddleServerSet.Length){
				// Wrap:
				_currentRand = 0;
			}
			
			return huddleServerSet[_currentRand++];
		}
		
		private int GetTimeSlice(DateTime timeUtc){
			return (int)((timeUtc.Subtract(_epoch)).TotalSeconds / TimeSliceSize);
		}

		/// <summary>
		/// Allocates a huddle server for the given time range and load factor.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="startTimeUtc"></param>
		/// <param name="projectedEndTimeUtc"></param>
		/// <param name="loadFactor"></param>
		/// <returns></returns>
		public async Task<HuddleServer> Allocate(Context context, DateTime startTimeUtc, DateTime projectedEndTimeUtc, int loadFactor)
		{
			if (huddleServerLookup == null || huddleServerLookup.Count == 0)
			{
				// Can't allocate yet (e.g. because there aren't any!)
				return null;
			}

			var allServers = new Dictionary<int, bool>();

			foreach (var kvp in huddleServerLookup)
			{
				allServers[kvp.Key] = true;
			}
				
			// Start and end times:
			var startSliceId = GetTimeSlice(startTimeUtc);
			var endSliceId = GetTimeSlice(projectedEndTimeUtc);

			if (endSliceId < startSliceId)
			{
				throw new Exception("Unable to schedule a huddle as it ends before it starts. Check your start and end times.");
			}

			StringBuilder query = new StringBuilder();
			query.Append(queryStart);
			
			for(var i=startSliceId;i<=endSliceId;i++){
				if(i != startSliceId){
					query.Append(',');
				}
				query.Append(i.ToString());
			}
			
			query.Append(") group by HuddleServerId order by sum(LoadFactor) asc");
			
			// Ask the DB for huddle load entries, grouped by server, across this range of time slices:
			var listQuery = Query.List<AllocatedHuddleServer>();
			listQuery.SetRawQuery(query.ToString());
			
			var allocations = await _database.List(null, listQuery, null);

			// Next, we need to find if there's any servers missing.
			foreach (var entry in allocations)
			{
				allServers.Remove(entry.HuddleServerId);
			}

			var serverToAllocateTo = 0;

			if (allServers.Count == 0)
			{
				// There's no missing servers.
				// The least busy one is allocations[0].
				serverToAllocateTo = allocations[0].HuddleServerId;
			}
			else
			{
				// There's servers which have no allocations at all during these time slices.
				// Pick a random one of those:
				serverToAllocateTo = allServers.ElementAt(rand.Next(0, allServers.Count)).Key;
			}

			// Allocating this server:
			if (!huddleServerLookup.TryGetValue(serverToAllocateTo, out HuddleServer targetServer))
			{
				return null;
			}

			// Update the load metric table:
			for (var i = startSliceId; i <= endSliceId; i++)
			{
				int sliceId = (int)(i | (serverToAllocateTo << 21));

				// Insert/ update each slice:
				var measurement = await _loadMetrics.Get(context, sliceId, DataOptions.IgnorePermissions);

				if (measurement == null)
				{
					// Doesn't exist, create it.
					await _loadMetrics.Create(context, new HuddleLoadMetric()
					{
						Id = sliceId,
						LoadFactor = loadFactor,
						HuddleServerId = serverToAllocateTo,
						TimeSliceId = i
					}, DataOptions.IgnorePermissions);
				}
				else
				{
					measurement.LoadFactor += loadFactor;
					await _loadMetrics.Update(context, measurement, DataOptions.IgnorePermissions);
				}

			}

			return targetServer;
		}

		/// <summary>
		/// 
		/// </summary>
		public class AllocatedHuddleServer
		{
			/// <summary>
			/// Allocated server ID.
			/// </summary>
			public int HuddleServerId;

			/// <summary>
			/// 
			/// </summary>
			public AllocatedHuddleServer() { }
			/// <summary>
			/// 
			/// </summary>
			/// <param name="huddleServerId"></param>
			public AllocatedHuddleServer(int huddleServerId) {
				HuddleServerId = huddleServerId;
			}
		}
	}
    
}
