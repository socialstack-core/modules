using Api.Database;

namespace Api.Payments
{
	[DatabaseIndex(Name= "Product_Audit_Creation_Date", Scope = "database", Unique=false, Fields = ["CreatedUtc"])]
	public partial class Product
	{
	
	}
}