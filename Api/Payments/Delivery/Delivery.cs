using System;
using System.Collections.Generic;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json;


namespace Api.Payments;

/// <summary>
/// A delivery. Uses the mapping called ProductQuantities to indicate what's in the delivery (same as an order).
/// </summary>
[ListAs("deliveries", Explicit = true)]
[ImplicitFor("deliveries", typeof(Purchase))]
public partial class Delivery : VersionedContent<uint>
{
	/// <summary>
	/// Expected delivery date/time. If only a date is known, set the time to 00:00.
	/// </summary>
	public DateTime ExpectedSlotUtc;

	/// <summary>
	/// A length, in minutes, of the time slot. For example, the slot start may represent 2pm and this is 30, indicating that  
	/// the delivery is expected between 2 - 2.30pm. Zero if it is not time slotted.
	/// </summary>
	public uint TimeWindowLength;

	/// <summary>
	/// Set when the delivery has actually happened and is the time noted on the delivery.
	/// </summary>
	public DateTime? ActualUtc;

	/// <summary>
	/// Can be e.g. "Royal Mail 24h".
	/// </summary>
	public string DeliveryName;

	/// <summary>
	/// Other general notes about this delivery.
	/// </summary>
	public string DeliveryNotes;

	/// <summary>
	/// The cost of the delivery itself (inc tax).
	/// </summary>
	public uint DeliveryCost;

	/// <summary>
	/// The cost of the delivery itself (less tax).
	/// </summary>
	public uint DeliveryCostLessTax;

	/// <summary>
	/// The cost of the delivery plus the items in it (inc tax).
	/// </summary>
	public uint TotalCost;

	/// <summary>
	/// The cost of the delivery plus the items in it (less tax).
	/// </summary>
	public uint TotalCostLessTax;

	/// <summary>
	/// The items in the delivery - this is only loaded and available during the BeforeCreate event of a delivery
	/// and the BeforeCreate of a Purchase.
	/// </summary>
	[JsonIgnore]
	public List<DeliveryItem> Items { get; set; }

	/// <summary>
	/// The effective budget time of this delivery. It is considered conditional and changes when the delivery happens.
	/// </summary>
	public DateTime GetBudgetEffectiveDateUtc()
	{
		if(ActualUtc.HasValue){
			return ActualUtc.Value;
		}
		
		return ExpectedSlotUtc;
	}
}