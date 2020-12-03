using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.DrawingCore;
using System.DrawingCore.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Api.Comments
{
	/// <summary>
	/// Handles comments.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class CommentService : AutoService<Comment>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public CommentService() : base(Events.Comment)
        {
			InstallAdminPages("Comments", "fa:fa-comment", new string[] { "id", "name" });
		}
	}
    
}
