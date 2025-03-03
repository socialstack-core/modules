using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;

namespace Api.ContentSync
{
	/// <summary>
	/// Handles an endpoint which describes the permissions on each role.
	/// </summary>

	[Route("v1/contentsync")]
	[ApiController]
	public partial class ContentSyncController : ControllerBase
	{
	}

}
