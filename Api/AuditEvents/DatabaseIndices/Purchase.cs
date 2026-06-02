using Api.Database;

namespace Api.Payments
{
	[DatabaseIndex(Name= "Purchase_Audit_Creation_Date", Scope = "database", Unique=false, Fields = ["CreatedUtc"])]
	public partial class Purchase
	{
	
	}
}