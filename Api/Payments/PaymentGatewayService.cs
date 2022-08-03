using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Payments
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PaymentGatewayService : AutoService
    {
		/// <summary>
		/// The list of gateways.
		/// </summary>
		public List<PaymentGateway> _allGateways = new List<PaymentGateway>();

		/// <summary>
		/// Lookup of gateway ID to the gateway instance.
		/// </summary>
		private Dictionary<uint, PaymentGateway> _idLookup = new Dictionary<uint, PaymentGateway>();

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public PaymentGatewayService()
		{
		}

		/// <summary>
		/// Gets a gateway by ID. Null if it does not exist.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public PaymentGateway Get(uint id)
		{
			_idLookup.TryGetValue(id, out var gateway);
			return gateway;
		}

		/// <summary>
		/// Adds a payment gateway.
		/// </summary>
		public void Register(PaymentGateway gateway)
		{
			_allGateways.Add(gateway);
			_idLookup[gateway.Id] = gateway;
		}
		
	}
    
}
