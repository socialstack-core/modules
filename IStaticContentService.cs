using Api.Contexts;
using Api.Permissions;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Uploader
{
	/// <summary>
	/// Manages static files.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public interface IStaticContentService
	{
		/// <summary>The admin index.html</summary>
		byte[] AdminIndex {get;}
		
		/// <summary>The frontend index.html</summary>
		byte[] Index {get;}

	}
}
