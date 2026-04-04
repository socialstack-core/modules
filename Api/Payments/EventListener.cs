using Api.AutoForms;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;

namespace Api.Payments;

/// <summary>
/// Adds custom payment behaviour
/// </summary>
[EventListener]
public partial class PaymentEventListener
{

    /// <summary>
    /// Event listeners for payments
    /// </summary>
    /// <exception cref="PublicException"></exception>
    public PaymentEventListener()
	{
		// Replace Product ContentSelect fields with custom Select that searches by SKU too
		// (not in ctor to guarantee that this is mounted whilst the service is initting).
		Events.AutoForm.GetFieldModule.AddEventListener(async (Context context, AutoFormField field, AutoService service) =>
		{
			if (field.Data != null && field.Data.TryGetValue("contentType", out var contentTypeObj) && contentTypeObj?.ToString() == "Product")
			{
				if (field.Includable)
				{
					field.Module = "Admin/Payments/Product/MultiSelect";
				}
				else
				{
					field.Module = "Admin/Payments/Product/Select";
				}
			}
			return field;
		});
	}
}