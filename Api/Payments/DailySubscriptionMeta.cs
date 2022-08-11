namespace Api.Payments;


/// <summary>
/// Metadata for the daily subscription automation.
/// </summary>
public struct DailySubscriptionMeta
{
	/// <summary>
	/// Set this to true if you know your subscriptions are not ready to be processed for some reason.
	/// This can be, for example, because your usage stats are not fully calculated yet.
	/// </summary>
	public bool DoNotProcess;

	/// <summary>
	/// The current month index.
	/// </summary>
	public uint MonthIndex;
	
	/// <summary>
	/// The current quarter index.
	/// </summary>
	public uint QuarterIndex;
	
	/// <summary>
	/// The current year index.
	/// </summary>
	public uint YearIndex;

}