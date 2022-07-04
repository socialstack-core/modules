using System;
using System.Diagnostics;
using System.Timers;


namespace Api.SocketServerLibrary {

	/// <summary>
	/// Helpers for getting very quick time references.
	/// </summary>
	public static class Time{
		
		private static Timer Clock;
		/// <summary>
		/// The current time.
		/// </summary>
		public static uint GlobalTime;
		/// <summary>The number of seconds the system had been on for when the offset was computed.</summary>
		private static int TickStartTime;
		/// <summary>The offset to add to TickCount when the time is requested.</summary>
		private static long TickCountOffset;
		/// <summary>The current unix time.</summary>
		public static uint UnixTime;
		/// <summary>
		/// The SocketServerLibrary epoch.
		/// </summary>
		private static DateTime StartDate=new DateTime(2020,5,1,0,0,0,DateTimeKind.Utc);
		private const long StartTicks = 637238880000000000; // StartTime.Ticks (constant).
		private static DateTime UnixStartDate=new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc);
		private const long UnixStartTicks=621355968000000000; // UnixStartdate.Ticks (constant).
		
		
		static Time(){
			Clock=new Timer();
			Clock.Elapsed+=OnTick;
			Clock.Interval=1000;
			Clock.Enabled=true;
			
			// Get current ticks:
			long ticks=DateTime.UtcNow.Ticks;
			
			// Set initial global time:
			GlobalTime=(uint)((ticks - StartTicks) / 10000000);
			
			// Unix time too:
			UnixTime=(uint)((ticks-UnixStartTicks) / 10000000);
			
			
			UpdateOffset();
		}
		
		/// <summary>Recomputes the TickCountOffset.</summary>
		private static void UpdateOffset(){
			// Grab the current UTC ticks as milliseconds:
			long currentTicks=DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
			
			// Next, find out how long the system has been up for:
			TickStartTime=Environment.TickCount;
			
			// Figure out the full tick offset. That's the current ticks - how long we've been on for.
			TickCountOffset=currentTicks-TickStartTime;
		}
		
		/// <summary>Gets the current number of milliseconds since 0000 as a global value.</summary>
		public static long Ticks{
			get{
				// Get the current tick count:
				int ticks=Environment.TickCount;
				
				// Did it wrap?
				if(ticks<TickStartTime){
					// Sure did! Figure out the offset again.
					UpdateOffset();
				}
				
				return ticks+TickCountOffset;
			}
		}
		
		private static void OnTick(object source,ElapsedEventArgs e){
			
			// Get current ticks:
			long ticks=DateTime.UtcNow.Ticks;
			
			// Set global time:
			GlobalTime=(uint)((ticks - StartTicks) / 10000000);
			
			// Unix time too:
			UnixTime=(uint)((ticks-UnixStartTicks) / 10000000);
			
		}
		
		/// <summary>
		/// Gets the unix time from a given datetime.
		/// </summary>
		/// <param name="dateTime"></param>
		/// <returns></returns>
		public static double UnixTimeFrom(DateTime dateTime){
			return (dateTime - UnixStartDate).TotalSeconds;
		}
		
	}
}