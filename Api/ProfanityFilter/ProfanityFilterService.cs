using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Configuration;
using Microsoft.Extensions.Configuration;

namespace Api.ProfanityFilter
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// A very simplistic profanity filter, which only matches whole words. Profanity filtering is generally a bad practise as it damages legitimate 
	/// communication whilst also not providing any friction at all - instead it's a fun challenge - to genuine trolls.
	/// </summary>
	public partial class ProfanityFilterService : AutoService
    {
		private ProfanityFilterConfig _configuration;
		
		/// <summary>Char to visually similar characters.</summary>
		public Dictionary<char,char[]> VisuallySimilar = new Dictionary<char, char[]>();

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ProfanityFilterService()
		{
			_configuration = GetConfig<ProfanityFilterConfig>();

			VisuallySimilar['a'] = new char[] { '@', '4' };
			VisuallySimilar['b'] = new char[] { '8' };
			VisuallySimilar['e'] = new char[] { '3' };
			VisuallySimilar['i'] = new char[] { '!' };
			VisuallySimilar['l'] = new char[] { '1' };
			VisuallySimilar['o'] = new char[] { '0' };
			VisuallySimilar['s'] = new char[] { '5', '$' };

			_configuration.OnChange += () => {
				LoadAllPatterns();
				return new ValueTask();
			};

			LoadAllPatterns();
		}

		private void LoadAllPatterns()
		{
			Root = new CharTreeNode();

			if (_configuration.Patterns == null)
			{
				return;
			}

			for (var i=0;i<_configuration.Patterns.Length;i++){
				AddPattern(_configuration.Patterns[i]);
			}
		}
		
		/// <summary>
		/// Adds given pattern to the tree.
		/// </summary>
		private void AddPattern(string pattern)
		{
			if(string.IsNullOrWhiteSpace(pattern))
			{
				return;
			}
			
			// Tree is lowercase to avoid case as a workaround:
			pattern = pattern.Trim().ToLower();
			var exclude = false;
			
			if(pattern[0] == '!')
			{
				// Exclusion
				exclude = true;
				pattern = pattern.Substring(1);
			}
			
			if(pattern[0] == '*'){
				pattern = pattern.Substring(1);
				// Backwards not supported atm
			}
			
			var anyEnding = pattern[pattern.Length-1] == '*';
			
			if(anyEnding){
				pattern = pattern.Substring(0, pattern.Length-1);
			}
			
			var node = Root;
			
			for(var i=0;i<pattern.Length;i++){
				// Break word down into a series of char nodes.
				node = node.GetOrCreate(pattern[i], VisuallySimilar);
			}

			if (exclude)
			{
				node.Exclude(pattern);
			}
			else
			{
				node.Terminal = anyEnding ? TerminalType.StartsWith : TerminalType.Exact;
			}
		}
		
		private CharTreeNode Root;
		
		/// <summary>
		/// Returns the number of profanity filter hits the given text has.
		/// </summary>
		public int Measure(string text)
		{
			if(string.IsNullOrEmpty(text))
			{
				return 0;
			}
			
			CharTreeNode active = Root;
			List<string> exclusions = null;
			TerminalType hitTerminal = TerminalType.None;
			int count = 0;
			int startIndex = 0;


			for (var i=0;i<text.Length;i++){
				var character = text[i];
				
				if(char.IsWhiteSpace(character))
				{
					// End current word.
					if(hitTerminal != TerminalType.None)
					{
						if (exclusions != null)
						{
							if (!CheckExcluded(exclusions, text.Substring(startIndex, i-startIndex)))
							{
								count++;
							}
						}
						else
						{
							count++;
						}
						hitTerminal = TerminalType.None;
						exclusions = null;
					}
					active = Root;
					startIndex = i + 1;
				}
				else if(active != null)
				{
					// Active is null if we don't care about the current word/phrase at all
					
					// Add lowercase variant to current word (toLower on a surrogate is a no-op):
					var next = active.Get(char.ToLower(character));
					
					if(hitTerminal == TerminalType.Exact)
					{
						if (char.IsPunctuation(character))
						{
							// Skip punctuation
							continue;
						}

						// We had arrived at a terminal. Only if next is equal to active (repeated character) is something we can ignore.
						if(next == active)
						{
							continue;
						}
						
						// This means there's stuff after the terminal that isn't the same letter being repeated
						// and isn't whitespace. Clear the terminal arrival, but continue using next if it's not null.
						// (If it was null, we end up skipping chars until the next whitespace).
						hitTerminal = TerminalType.None;
						exclusions = null;
					}
					
					active = next;
					
					if(active != null && hitTerminal == TerminalType.None)
					{
						// Set terminal state (can be none):
						hitTerminal = active.Terminal;
						exclusions = active.Exclusions;
					}
				}
			}
			
			if(hitTerminal != TerminalType.None){
				if (exclusions != null)
				{
					if (!CheckExcluded(exclusions, text.Substring(startIndex)))
					{
						count++;
					}
				}
				else
				{
					count++;
				}

			}
			
			return count;
		}

		private bool CheckExcluded(List<string> exclusions, string substr)
		{
			substr = substr.ToLower();

			for (var i = 0; i < exclusions.Count; i++)
			{
				if (exclusions[i] == substr)
				{
					return true;
				}
			}

			return false;
		}

	}
    
	/// <summary>
	/// A node in a character tree.
	/// </summary>
	public class CharTreeNode{
		/// <summary>True if this is a termination point. If only punctuation matches after a 
		/// terminal point, the match was successful. Can also be "starts with" meaning anything that matches afterwards is also a success.</summary>
		public TerminalType Terminal = TerminalType.None;
		/// <summary>The character.</summary>
		public char Character;
		/// <summary>
		/// Alt characters that are visually similar.
		/// </summary>
		public char[] Alts;
		
		/// <summary> Nodes that follow this one.</summary>
		public Dictionary<char, CharTreeNode> Followers = new Dictionary<char, CharTreeNode>();

		/// <summary>
		/// Patterns which are excluded if we hit this terminal.
		/// </summary>
		public List<string> Exclusions = null;

		/// <summary>
		/// Excludes the given lowercase pattern, if it's an exact match in the backbuffer.
		/// </summary>
		/// <param name="patternLC"></param>
		public void Exclude(string patternLC)
		{
			Exclusions = new List<string>();
			Exclusions.Add(patternLC);
		}

		/// <summary>
		/// Appends to the tree the given char.
		/// </summary>
		public CharTreeNode GetOrCreate(char character, Dictionary<char, char[]> synonyms)
		{
			if(character == Character)
			{
				// Repetition is ignored to permit a variety of typo's:
				return this;
			}

			if (Alts != null)
			{
				for (var i = 0; i < Alts.Length; i++)
				{
					if (character == Alts[i])
					{
						return this;
					}
				}
			}

			// otherwise get or create from Followers:
			if(!Followers.TryGetValue(character, out CharTreeNode result))
			{
				result = new CharTreeNode(){
					Character = character
				};
				
				Followers[character] = result;
				
				// Add all visually similar matches:
				if(synonyms.TryGetValue(character, out char[] charSet))
				{
					result.Alts = charSet;

					for(var i=0;i<charSet.Length;i++)
					{
						Followers[charSet[i]] = result;
					}
				}
			}
			
			return result;
		}
		
		/// <summary>
		/// Advances through the tree if possible.
		/// </summary>
		public CharTreeNode Get(char follower)
		{
			if(follower == Character)
			{
				return this;
			}

			if (Alts != null)
			{
				for (var i = 0; i < Alts.Length; i++)
				{
					if (follower == Alts[i])
					{
						return this;
					}
				}
			}

			Followers.TryGetValue(follower, out CharTreeNode result);
			return result;
		}
	}
	
	/// <summary>
	/// Available terminal types
	/// </summary>
	public enum TerminalType{
		/// <summary>
		/// Not a terminal
		/// </summary>
		None,
		/// <summary>
		/// Word starts with this
		/// </summary>
		StartsWith,
		/// <summary>
		/// Word must terminate here to match
		/// </summary>
		Exact
	}
	
}
