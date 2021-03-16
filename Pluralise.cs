using Api.Eventing;
using System;
using System.Collections.Generic;
using System.Transactions;
using System.Text.RegularExpressions;


namespace Api.Startup
{
	/// <summary>
	/// Helper class for handling basic plurals of words (usually content types). 
	/// Not intended to be completely accurate; just fast and accurate for the vast majority of words it will encounter.
	/// </summary>
	public static class Pluralise
	{
		/// <summary>
		/// Exception list.
		/// </summary>
		private static Dictionary<string, string> exceptions;
		
		/// <summary>
		/// Uppercases first letter of word.
		/// </summary>
		public static string FirstLetterToUpper(string str)
		{
			if (str == null)
				return null;
			
			if (str.Length > 1)
				return char.ToUpper(str[0]) + str.Substring(1);
			
			return str.ToUpper();
		}

		private static Regex _niceName = new Regex("([^A-Z])([A-Z])");

		/// <summary>
		/// Adds spaces into the given text. E.g. "BlogPost" becomes "Blog Post".
		/// </summary>
		public static string NiceName(string text)
		{
			text = _niceName.Replace(text, "$1 $2");
			return char.ToUpper(text[0]) + text.Substring(1);
		}
		
		/// <summary>
		/// Attempts to pluralize the specified text according to the rules of the English language.
		/// </summary>
		/// <remarks>
		/// This function attempts to pluralize as many words as practical by following these rules:
		/// <list type="bullet">
		///		<item><description>Words that don't follow any rules (e.g. "mouse" becomes "mice") are returned from a dictionary.</description></item>
		///		<item><description>Words that end with "y" (but not with a vowel preceding the y) are pluralized by replacing the "y" with "ies".</description></item>
		///		<item><description>Words that end with "us", "ss", "x", "ch" or "sh" are pluralized by adding "es" to the end of the text.</description></item>
		///		<item><description>Words that end with "f" or "fe" are pluralized by replacing the "f(e)" with "ves".</description></item>
		///	</list>
		/// </remarks>
		/// <param name="text">The text to pluralize.</param>
		/// <returns>A string that consists of the given singular word in its pluralized form, in lowercase.</returns>
		public static string Apply(string text)
		{
			// Create a dictionary of exceptions that have to be checked first
			// This is very much not an exhaustive list!
			if(exceptions == null)
			{
				exceptions = new Dictionary<string, string>() {
				{ "man", "men" },
				{ "woman", "women" },
				{ "child", "children" },
				{ "tooth", "teeth" },
				{ "goose", "geese" },
				{ "foot", "feet" },
				{ "mouse", "mice" },
				{ "person", "people" },
				{ "fish", "fish" },
				{ "deer", "deer" },
				{ "sheep", "sheep" },
				{ "shrimp", "shrimp" },
				{ "swine", "swine" },
				{ "moose", "moose" },
				{ "buffalo", "buffalo" },
				{ "trout", "trout" },
				{ "belief", "beliefs" } };
			}
			
			var lower = text.ToLowerInvariant();
			var last = lower[lower.Length - 1];
			var lastTwo = lower.Length > 2 ? lower.Substring(lower.Length - 2) : string.Empty;
			
			string result;
			
			if (exceptions.TryGetValue(lower, out result))
			{
				return result;
			}
			
			if (last == 'y' &&
				lastTwo != "ay" &&
				lastTwo != "ey" &&
				lastTwo != "iy" &&
				lastTwo != "oy" &&
				lastTwo != "uy"
			){
				return lower.Substring(0, lower.Length - 1) + "ies";
			}
			else if (lastTwo == "us" || lastTwo == "ss" || last == 'o')
			{
				// http://en.wikipedia.org/wiki/Plural_form_of_words_ending_in_-us
				return lower + "es";
			}
			else if (last == 's' || lastTwo == "es")
			{
				return lower;
			}
			else if (last == 'x' ||
				lastTwo == "ch" ||
				lastTwo == "sh")
			{
				return lower + "es";
			}
			else if (last == 'f')
			{
				return lower.Substring(0, lower.Length - 1) + "ves";
			}
			else if (lastTwo == "fe")
			{
				return lower.Substring(0, lower.Length - 2) + "ves";
			}
			else if(lower.EndsWith("craft"))
			{
				return lower;
			}
			else
			{
				return lower + "s";
			}
		}
	}
}
