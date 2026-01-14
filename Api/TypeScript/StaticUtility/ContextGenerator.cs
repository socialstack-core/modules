using System.Collections.Generic;
using Api.Contexts;
using Api.Startup;
using System.IO;
using System.Text;

namespace Api.TypeScript;


/// <summary>
/// Generates Session.tsx from the C# Context metadata.
/// </summary>
public static class ContextGenerator{

	/// <summary>
	/// Generates the typescript file.
	/// </summary>
	/// <returns></returns>
	public static string Generate()
	{
		var sb = new StringBuilder();
		sb.Append("import { ApiContent } from 'UI/Functions/WebRequest';\r\n");

        var contextService = Services.Get<ContextService>();

        var imports = new HashSet<string>();

		// First, add each import:
		foreach (var field in ContextFields.FieldList)
		{
			// No visibility considerations needed here.
			// If it's in the field list, we show it.
			
			var contentType = field.ContentType.Name;

			if (!imports.Add(contentType))
			{
				continue;
			}
			
			sb.Append("import { ");
			sb.Append(contentType);
			sb.Append(" } from 'Api/");
			sb.Append(contentType);
			sb.Append("';\r\n");
		}

		sb.Append(@"// File generated from the C# definition

declare global {

    interface Session {

        /**
         * Set when the UI is currently waiting for the sessions user info to load.
         */
        loadingUser?: Promise<Session>

        /**
         * Optionally provided as the 'real' user when the current user is impersonating someone else.
         * Use sparingly as overuse would of course make impersonation relatively meaningless.
         */
        realUser?: User
");

		// Add each field next.
		foreach (var field in ContextFields.FieldList)
		{
			// No visibility considerations needed here.
			// If it's in the field list, we show it.

			var contentType = field.ContentType.Name;

			sb.Append(@"
		/**
         * The current session " + contentType + @"
         */
        ");
			sb.Append(field.JsonFieldName);
			sb.Append("?: ");
			sb.Append(contentType);
			sb.Append("\r\n");
		}

		sb.Append(@"
    }");
		sb.Append("\r\n");

		// Next we need to define SessionResponse.
		// This exists because the server does not pre-expand includes 
		// and thus the actual json from the server is a little different from 
		// an actual session.
		sb.Append("    interface SessionResponse {\r\n");
		
		foreach (var field in ContextFields.FieldList)
		{
			// No visibility considerations needed here.
			// If it's in the field list, we show it.

			var contentType = field.ContentType.Name;

			sb.Append(@"
		/**
         * The pre-expanded " + contentType + @"
         */
        ");
			sb.Append(field.JsonFieldName);
			sb.Append("?: ApiContent<");
			sb.Append(contentType);
			sb.Append(">\r\n");
		}

		sb.Append(@"
	}
}

export { };");

		return sb.ToString();
	}

	/// <summary>
	/// Writes the session info to the given file.
	/// </summary>
	/// <param name="filename"></param>
	public static void SaveToFile(string filename)
	{
		var dir = Path.GetDirectoryName(filename);
		Directory.CreateDirectory(dir); // noop if it doesn't exist
		var str = Generate();
		File.WriteAllText(filename, str);
	}

}