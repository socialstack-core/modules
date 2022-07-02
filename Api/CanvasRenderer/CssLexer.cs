namespace Api.Themes
{
	
	/// <summary>
	/// A simple CSS lexer with the duty of identifying selectors and the properties applied to them.
	/// </summary>
	public struct CssLexer
	{
		/// <summary>
		/// Raw CSS
		/// </summary>
		public string Text;
		/// <summary>
		/// Current index in the lexer.
		/// </summary>
		public int Index;
		
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		public CssLexer(string text)
		{
			Text = text;
			Index = 0;

			// If starts with BOM, skip it.
			if (text.Length > 1 && text[0] == '\uFEFF')
			{
				Index = 1;
			}

		}
		
		/// <summary>
		/// Skips 1 char.
		/// </summary>
		public void Skip()
		{
			Index++;
		}
		
		/// <summary>
		/// True if there's more in this lexer.
		/// </summary>
		public bool More()
		{
			return Index<Text.Length;
		}
		
		/// <summary>
		/// Skips junk such as spaces or newlines (don't use this when inside a "string" scope).
		/// </summary>
		public void SkipJunk(bool skipSpaces)
		{
			while (Index < Text.Length)
			{
				var c = Text[Index];
				if (c == '\r' || c == '\n' || c == '\t')
				{
					Index++;
				}
				else if (skipSpaces && c == ' ')
				{
					Index++;
				}
				else
				{
					break;
				}
			}
		}
		
		/// <summary>
		/// Current char. Reading this advances by 1.
		/// </summary>
		public char Current
		{
			get{
				var result = Text[Index];
				Index++;
				return result;
			}
		}
		
		/// <summary>
		/// Peeks the next character, optionally n in the distance. Returns a nul byte if it is beyond the EOS.
		/// </summary>
		public char Peek(int offset = 0)
		{
			offset += Index;
				
			if(offset >= Text.Length)
			{
				return '\0';
			}
			
			return Text[offset];
		}
		
	}
	
}