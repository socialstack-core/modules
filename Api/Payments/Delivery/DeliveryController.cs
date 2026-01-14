using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Payments;

/// <summary>Handles delivery endpoints.</summary>
[Route("v1/delivery")]
public partial class DeliveryController : AutoController<Delivery>
{

}