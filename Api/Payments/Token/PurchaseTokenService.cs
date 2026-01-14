using System;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Eventing;

namespace Api.Payments
{
	/// <summary>
	/// Handles purchaseTokens.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PurchaseTokenService : AutoService<PurchaseToken>
	{
		/// <summary>
		/// Request expiry time, in hours.
		/// </summary>
		public const int DefaultExpiryTime = 48;
			
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PurchaseTokenService() : base(Events.PurchaseToken)
		{
			
			Events.PurchaseToken.BeforeCreate.AddEventListener(async (Context context, PurchaseToken purchaseToken) => {
				
				if(purchaseToken == null)
                {
					return null;
				}
				
				// Generate a token
				purchaseToken.Token = await GetToken(context);
				purchaseToken.CreatedUtc = DateTime.UtcNow;
				
				if (purchaseToken.CreatedUtc == default) {
				    purchaseToken.CreatedUtc = DateTime.UtcNow;
				}

				if (purchaseToken.ExpiresUtc == default) {
				    purchaseToken.ExpiresUtc = DateTime.UtcNow.AddHours(DefaultExpiryTime);
				}

				return purchaseToken;
			}, 5);

			Events.PurchaseTokenAfterUse.AddEventListener(async (Context context, PurchaseToken purchaseToken, string IpAddress) => {

				if (purchaseToken == null)
				{
					return null;
				}

				purchaseToken = await Update(context, purchaseToken, (ctx, toUpdate, orig) =>
				{
					toUpdate.UsedCount = orig.UsedCount + 1;
					if (!string.IsNullOrWhiteSpace(IpAddress) && (orig.IpAddress == null || !orig.IpAddress.Contains(IpAddress))) {
						toUpdate.IpAddress = orig.IpAddress + "::" + IpAddress;
					}

				}, DataOptions.IgnorePermissions);

				return purchaseToken;
			}, 5);
		}
		
		/// <summary>
		/// True if given req has expired.
		/// <param name="purchaseToken"></param>
		/// </summary>
		public bool HasExpired(PurchaseToken purchaseToken)
		{
			// Either doesn't exist, or its invalid/expired
			return (purchaseToken == null || (purchaseToken.IsSingleUse && purchaseToken.UsedCount > 0) || purchaseToken.ExpiresUtc < DateTime.UtcNow);
		}

		/// <summary>
		/// Gets a purchase token. This overload is always permitted (be careful!).
		/// </summary>
		/// <param name="context"></param>
		/// <param name="token"></param>
		/// <param name="ipAddress"></param>
		/// <returns></returns>
		public async Task<PurchaseToken> Get(Context context, string token, string ipAddress = null)
		{
			var purchaseToken = await Where("Token=?", DataOptions.IgnorePermissions).Bind(token).First(context);

			if (HasExpired(purchaseToken))
            {
                return null;
            }

			purchaseToken = await Events.PurchaseTokenAfterUse.Dispatch(context, purchaseToken, ipAddress);

			return purchaseToken;
		}

		/// <summary>
		/// Get a unique token 
		/// </summary>
		/// <returns></returns>
		private async ValueTask<string> GetToken(Context context)
		{
			// Let's generate a token
			var token = RandomToken.Generate();

			// Is this unique?
			var existingToken = await Where("Token=?", DataOptions.IgnorePermissions).Bind(token).First(context);

			while (existingToken != null)
			{
				// Let's reroll and check again
				token = RandomToken.Generate();
				existingToken = await Where("Token=?", DataOptions.IgnorePermissions).Bind(token).First(context);
			}
			return token;
		}
	}
}
