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
	public partial class ReactionTypeService : AutoService<ReactionType>
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ReactionTypeService() : base(Events.ReactionType)
		{
			InstallAdminPages("Reactions", "fa:fa-thumbs-up", new string[] { "id", "name" });

			// Because of IHaveReactions, Reaction types must be nestable:
			MakeNestable();

			Task.Run(async () =>
			{

				var reactionTypeList = Query.List<ReactionType>();
				reactionTypeList.Where().PageSize = 1;

				var types = await _database.List(null, reactionTypeList, null);

				if (types.Count == 0)
				{
					// The table is completely empty - let's install the defaults now.
					var defaults = new ReactionType[] {

						new ReactionType(){
							Name = "Like",
							Key = "like",
							IconRef = "emoji:1F44D",
							GroupId = 1
						},
						new ReactionType(){
							Name = "Dislike",
							Key = "dislike",
							IconRef = "emoji:1F44E",
							GroupId = 1
						},
						new ReactionType(){
							Name = "Upvote",
							Key = "upvote",
							IconRef = "emoji:1F53C",
							GroupId = 2
						},
						new ReactionType(){
							Name = "Downvote",
							Key = "downvote",
							IconRef = "emoji:1F53D",
							GroupId = 2
						},
						new ReactionType(){
							Name = "Heart",
							Key = "heart",
							IconRef = "emoji:2764,FE0F",
							GroupId = 0
						},
						new ReactionType(){
							Name = "Stuck out tongue",
							Key = "stuck_out_tongue",
							IconRef = "emoji:1F61B",
							GroupId = 0
						},
						new ReactionType(){
							Name = "Slightly smiling face",
							Key = "slightly_smiling_face",
							IconRef = "emoji:1F642",
							GroupId = 0
						},
						new ReactionType(){
							Name = "Smile",
							Key = "smile",
							IconRef = "emoji:263A,FE0F",
							GroupId = 0
						},
						new ReactionType(){
							Name = "Tada",
							Key = "tada",
							IconRef = "emoji:1F389",
							GroupId = 0
						},
						new ReactionType(){
							Name = "Stuck out tongue winking",
							Key = "stuck_out_tongue_winking_eye",
							IconRef = "emoji:1F61C",
							GroupId = 0
						},
						new ReactionType(){
							Name = "Ok hand",
							Key = "ok_hand",
							IconRef = "emoji:1F44C",
							GroupId = 0
						},
						new ReactionType(){
							Name = "Clap",
							Key = "clap",
							IconRef = "emoji:1F44F",
							GroupId = 0
						},
						new ReactionType(){
							Name = "Beers",
							Key = "beers",
							IconRef = "emoji:1F37B",
							GroupId = 0
						},
						new ReactionType(){
							Name = "Rainbow",
							Key = "rainbow",
							IconRef = "emoji:1F308",
							GroupId = 0
						},
						new ReactionType(){
							Name = "1 Star",
							Key = "1_star",
							IconRef = "emoji:2B50",
							GroupId = 3
						},
						new ReactionType(){
							Name = "2 Star",
							Key = "2_star",
							IconRef = "emoji:2B50",
							GroupId = 3
						},
						new ReactionType(){
							Name = "3 Star",
							Key = "3_star",
							IconRef = "emoji:2B50",
							GroupId = 3
						},
						new ReactionType(){
							Name = "4 Star",
							Key = "4_star",
							IconRef = "emoji:2B50",
							GroupId = 3
						},
						new ReactionType(){
							Name = "5 Star",
							Key = "5_star",
							IconRef = "emoji:2B50",
							GroupId = 3
						},
						new ReactionType(){
							Name = "6 Star",
							Key = "6_star",
							IconRef = "emoji:2B50",
							GroupId = 3
						},
						new ReactionType(){
							Name = "7 Star",
							Key = "7_star",
							IconRef = "emoji:2B50",
							GroupId = 3
						},
						new ReactionType(){
							Name = "8 Star",
							Key = "8_star",
							IconRef = "emoji:2B50",
							GroupId = 3
						},
						new ReactionType(){
							Name = "9 Star",
							Key = "9_star",
							IconRef = "emoji:2B50",
							GroupId = 3
						},
						new ReactionType(){
							Name = "10 Star",
							Key = "10_star",
							IconRef = "emoji:2B50",
							GroupId = 3
						}

					};

					var context = new Context();

					foreach(var defaultType in defaults)
					{
						await Create(context, defaultType);
					}
				}
			});
		}
	}
}