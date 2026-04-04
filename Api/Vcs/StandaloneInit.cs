using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Api.Vcs;


/// <summary>
/// Instanced automatically. Handles the 'git' command line function.
/// </summary>
[EventListener]
public class StandaloneInit{
	
	/// <summary>
	/// Instanced automatically.
	/// </summary>
	public StandaloneInit()
	{

		Events.Service.CommandLine.AddEventListener((Context context, CommandArgs args) => {

			if (args == null || args.Handled)
			{
				return new ValueTask<CommandArgs>(args);
			}

			if (args.Operation != "git")
			{
				return new ValueTask<CommandArgs>(args);
			}

			// Services.BuildHost = "git";
			// Services.RegisterAndStart();

			// this isn't a GIT replacement, simply fires off when hooks are executed. 
			// this allows the ecosystem to run scripts in C# rather than node, which is useful for those who don't have node installed.

			// var svc = Services.Get<GitService>();
			string[] validOptions = ["pre-commit", "commit-msg", "pre-push"];

			var rawArgs = args.Arguments;

			if (rawArgs.Length == 1)
			{
				// no follow up args, this should just exit out with an error message.
				throw new InvalidOperationException("You must provide an option after 'dotnet run git'. Options are: " + string.Join(", ", validOptions));
			}

			var option = rawArgs[1].ToLowerInvariant();

			if (!validOptions.Contains(option))
			{
				throw new InvalidOperationException("Invalid option provided after 'dotnet run git'. Options are: " + string.Join(", ", validOptions));
			}

			// switch(option)
			// {
			//     // case "pre-commit":
			//     //     // run pre-commit hooks:
			//     //     svc.RunPreCommit().GetAwaiter().GetResult();
			//     //     break;
			//     // case "commit-msg":
			//     //     // run commit-msg hooks:
			//     //     svc.RunCommitMessage().GetAwaiter().GetResult();
			//     //     break;
			//     // case "pre-push":
			//     //     // run pre-push hooks:
			//     //     svc.RunPrePush().GetAwaiter().GetResult();
			//     //     break;
			// }

			args.Handled = true;
			return new ValueTask<CommandArgs>(args);
		});

	}
}