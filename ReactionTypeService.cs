using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Reactions
{
	/// <summary>
	/// Handles reaction types - i.e. define a new type of reaction that can be used on content.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ReactionTypeService : AutoService<ReactionType>, IReactionTypeService
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ReactionTypeService() : base(Events.ReactionType)
		{
			InstallAdminPages("Reactions", "fa:fa-thumbs-up", new string[] { "id", "name" });
		}
	}
}