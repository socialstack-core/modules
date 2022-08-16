using System;

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
	/// The exact date line where subs are being handled from/ to.
	/// </summary>
	public DateTime ProcessDateUtc;
}