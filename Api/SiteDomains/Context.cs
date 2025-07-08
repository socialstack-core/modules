using System.Threading.Tasks;
using Api.Startup;
using Api.SiteDomains;

namespace Api.Contexts
{
	public partial class Context
	{
		
		private static SiteDomainService _siteDomainService;

		/// <summary>
		/// Underlying site domain id
		/// </summary>
		private uint _siteDomainId = 0;
		
		/// <summary>
		/// The current site domain id
		/// </summary>
		public uint SiteDomainId
		{
			get
			{
				return _siteDomainId;
			}
			set
			{

                _siteDomain = null;
                _siteDomainId = value;
			}
		}

		/// <summary>
		/// The full domain object, if it has been requested.
		/// </summary>
		private SiteDomain _siteDomain;

		/// <summary>
		/// Gets the domain for this context.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<SiteDomain> GetSiteDomain()
		{
			if (SiteDomainId == 0)
			{
				return null;
			}

			if (_siteDomain != null)
			{
				return _siteDomain;
			}

			if (_siteDomainService == null)
			{
                _siteDomainService = Services.Get<SiteDomainService>();
			}

            // Get the site domain
            _siteDomain = await _siteDomainService.Get(this, SiteDomainId, DataOptions.IgnorePermissions);


			return _siteDomain;
		}
		
	}
}