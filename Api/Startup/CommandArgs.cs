using System;

namespace Api.Startup;

/// <summary>
/// Used when at least 1 command arg has been provided.
/// </summary>
public class CommandArgs
{
	/// <summary>
	/// The raw args themselves.
	/// </summary>
	public string[] Arguments;

	/// <summary>
	/// Set this to true if your event handled this particular arg set.
	/// </summary>
	public bool Handled;

	/// <summary>
	/// Set this to false if you don't want the application to shutdown after your arg handler runs.
	/// When it's false, the web service then starts up normally.
	/// </summary>
	public bool Shutdown = true;

	/// <summary>
	/// The main operation.
	/// </summary>
	public string Operation => Arguments[0];

	/// <summary>
	/// Gets a named flag which is of the form "-flagName" or "--flagName" followed by a value.
	/// Basic arg scan here at the moment.
	/// </summary>
	/// <param name="flagName"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public string GetFlag(string flagName, string defaultValue)
	{
		// Basic flag check - just scan along args for -flagName and then returns the value after it.
		var dashedFlag = "-" + flagName;
		var doubleDashedFlag = "--" + flagName;

		for (var i = 1; i < Arguments.Length; i++)
		{
			var arg = Arguments[i];

			if (arg == null || arg[0] != '-')
			{
				continue;
			}

			arg = arg.ToLower();

			if (arg == dashedFlag || arg == doubleDashedFlag)
			{
				// If there is no following arg then we must crash to avoid any ambiguities.
				if (i == Arguments.Length - 1)
				{
					throw new ArgumentException(flagName + " is present but has no specified value.");
				}

				return Arguments[i+1];
			}
		}

		return defaultValue;
	}
}
