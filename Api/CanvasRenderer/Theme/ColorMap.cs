using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;


namespace Api.Themes
{
	
	/// <summary>
	/// Used to map colour name to colour value.
	/// </summary>
	
	public static class ColorMap
	{
		/// <summary>A map from colour name to hex value.</summary>
		private static Dictionary<string,Color> Map;
		
		
		/// <summary>
		/// Loads a colour from a CSS value using a simplistic parsing technique which only supports the color values in the spec.
		/// </summary>
		public static Color FromCss(string value)
		{
			if(string.IsNullOrWhiteSpace(value))
			{
				return new Color(0,0,0); // Black
			}
			
			value = value.Trim().ToLower();
			
			var argStart = value.IndexOf('(');
			
			if(argStart == -1)
			{
				// can't be rgb/ hsl. It's either hex or named.
				
				// Try to get the colour by name:
				bool recognised;
				Color colour = GetColorByName(value, out recognised);
				
				if(recognised){
					return colour;
				}
				
				// Get as hex:
				return GetHexColor(value);
			}

			argStart++;

			// Which function is it? There are 4 color funcs currently.
			if (value.StartsWith("hsl")) // Also hsla
			{
				var h = ReadNumericValue(value, ref argStart);
				var s = ReadNumericValue(value, ref argStart);
				var l = ReadNumericValue(value, ref argStart);
				var a = ReadNumericValue(value, ref argStart);

				var hV = h.ToPercent(360f);
				var sV = s.ToPercent(1f);
				var lV = l.ToPercent(1f);

				return Color.FromHsl(
					hV,
					sV,
					lV,
					a == CssNumericValue.None ? 1f : a.ToPercent(1f)
				);

			}
			else if (value.StartsWith("rgb")) // Also rgba
			{
				var r = ReadNumericValue(value, ref argStart);
				var g = ReadNumericValue(value, ref argStart);
				var b = ReadNumericValue(value, ref argStart);
				var a = ReadNumericValue(value, ref argStart);

				return new Color(
					r.ToPercent(255f),
					g.ToPercent(255f),
					b.ToPercent(255f),
					a == CssNumericValue.None ? 1f : a.ToPercent(1f)
				);
			}
			
			// Black otherwise.
			return new Color(0,0,0);
		}

		/// <summary>
		/// Reads a numeric CSS value.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		private static CssNumericValue ReadNumericValue(string value, ref int index)
		{
			// Whitespace skip:
			while (index < value.Length && value[index] == ' ')
			{
				index++;
				continue;
			}

			if (index >= value.Length)
			{
				// This typically happens when alpha is omitted. It has an implied value.
				return CssNumericValue.None;
			}

			// Read numbers or % up to , or )

			float currentNumber = 0f;
			var isPerc = false;
			var dpCounter = 0f;

			while (index < value.Length)
			{
				var current = value[index];
				index++;

				if (current == ' ' || current == '\r' || current == '\n')
				{
					continue;
				}

				if (current == ',' || current == ')')
				{
					// terminal
					break;
				}

				if (current == '%')
				{
					isPerc = true;
				}
				else if (current == '.')
				{
					dpCounter = 1f;
				}
				else if (current >= '0' && current <= '9')
				{
					if (dpCounter != 0f)
					{
						dpCounter *= 10f;
					}

					// Move it along:
					currentNumber *= 10;
					currentNumber += (current - '0');
				}
				else
				{
					// Invalid, return 0
					return CssNumericValue.None;
				}
			}

			if (dpCounter != 0)
			{
				currentNumber /= dpCounter;
			}

			if (isPerc)
			{
				currentNumber /= 100f;
			}

			return new CssNumericValue(currentNumber, isPerc);
		}

		/// <summary>Loads a hex value from the given 'any' string. Non-hex chars are treated as 0.
		/// (Normally, non-hex chars invalidate the whole thing).</summary>
		private static int LoadHexFromPart(string randomString, int startOffset, int length)
		{
			bool firstChar=true;
			int result=0;
			
			// Hex chars are in the range:
			// 48->57 and 97->102
			
			// For each char in our substring..
			for(int i=startOffset;i<length;i++)
			{
				int cc=randomString[i];

				if (cc > 48 && cc <= 57)
				{
					// In hex range (0-9)
					if(firstChar)
					{
						firstChar=false;
						result+=(cc-48) * 16;
					}
					else
					{
						result+=(cc-48);
						
						// Truncate the rest:
						break;
					}
				}
				else if(cc>=97 && cc<=102)
				{
					
					// In hex range (A-F)
					
					if(firstChar)
					{
						firstChar=false;
						result+=(cc-87) * 16;
					}
					else
					{
						result+=(cc-87);
						
						// Truncate the rest:
						break;
					}
					
				}
				else
				{
					
					// A zero.
					if(firstChar)
					{
						firstChar=false;
					}
					else
					{
						// Truncate the rest:
						break;
					}
					
				}
				
			}
			
			if((length-startOffset)==1)
			{
				// Duplicate:
				result+=(result>>4);
			}
			
			return result;
		}
		
		/// <summary>Maps colour name, e.g. aqua, to colour. Must be lowercase.</summary>
		/// <returns>An uppercase hex string or null if not found.</returns>
		public static Color GetColorByName(string name, out bool success)
		{
			if(Map == null){
				Map=new Dictionary<string, Color>();
				
				int colourCount = Colors.Length;
				
				for(int i=0;i<colourCount;i+=2)
				{
					// Get name and colour:
					string n=Colors[i];
					string c=Colors[i+1];
					
					// Add to map:
					Map[n.ToLower()]=GetHexColor(c);
				}
			}
			
			success=Map.TryGetValue(name,out Color colour);
			return colour;
		}
		
		/// <summary>Duplicates the given nibble (4 bit number) and places the result alongside in the same byte.
		/// E.g. c in hex becomes cc.</summary>
		/// <param name="nibble">The nibble to duplicate.</param>
		private static int DoubleNibble(int nibble)
		{
			return ((nibble<<4) | nibble);
		}
		
		/// <summary>Gets a colour from a hex HTML string.</summary>
		public static Color GetHexColor(string valueText){
			
			if(valueText[0]=='#'){
				valueText=valueText.Substring(1);
			}

			GetHexColor(valueText,out float r,out float g,out float b,out float a);
			
			return new Color(r,g,b,a);
			
		}
		
		/// <summary>Gets a colour from a hex HTML string.</summary>
		public static void GetHexColor(string valueText,out float r,out float g,out float b,out float a){
			
			int rI;
			int gI;
			int bI;
			int aI;
			GetHexColor(valueText,out rI,out gI,out bI,out aI);
			
			r=rI / 255f;
			g=gI / 255f;
			b=bI / 255f;
			a=aI / 255f;
		}
		
		/// <summary>Gets a colour from a hex HTML string.</summary>
		public static void GetHexColor(string valueText,out int r,out int g,out int b,out int a){
			
			int temp;
			
			int length=valueText.Length;
			
			if(length==3){
				// Shorthand hex colour, e.g. #f0f. Each character is essentially duplicated.
				
				// R:
				int.TryParse(valueText.Substring(0,1),NumberStyles.HexNumber,null,out temp);
				r=DoubleNibble(temp);
				// G:
				int.TryParse(valueText.Substring(1,1),NumberStyles.HexNumber,null,out temp);
				g=DoubleNibble(temp);
				// B:
				int.TryParse(valueText.Substring(2,1),NumberStyles.HexNumber,null,out temp);
				b=DoubleNibble(temp);
				
				a=255;
				return;
				
			}
			// Full hex colour, possibly also including alpha.
			
			if(length>=2){
				int.TryParse(valueText.Substring(0,2),NumberStyles.HexNumber,null,out r);
			}else{
				r=0;
			}
			
			if(length>=4){
				int.TryParse(valueText.Substring(2,2),NumberStyles.HexNumber,null,out g);
			}else{
				g=0;
			}
			
			if(length>=6){
				int.TryParse(valueText.Substring(4,2),NumberStyles.HexNumber,null,out b);
			}else{
				b=0;
			}
			
			if(length>=8){
				int.TryParse(valueText.Substring(6,2),NumberStyles.HexNumber,null,out a);
			}else{
				a=255;
			}
			
		}

		/// <summary>Converts any random string into a colour. This is a strange part of the specs
		/// that rarely gets used now, but it makes for some interesting colours ("grass" is green!).
		/// For example, body bgcolor uses this.</summary>
		public static Color ToSpecialColor(string randomString)
		{
			// Trim and lowercase:
			randomString = randomString.Trim().ToLower();

			if (randomString.StartsWith("#"))
			{
				randomString = randomString.Substring(1);
			}

			// Recognised colour?
			var col = GetColorByName(randomString, out bool recognised);

			if (recognised)
			{
				return col;
			}

			// Time to go a bit odd! http://randomstringtocsscolor.com/ 
			// has a nice explanation of this algorithm.
			// (Although we do it in a slightly different order for better performance)

			int length = randomString.Length;

			if (length == 0)
			{
				return new Color(0, 0, 0);
			}

			// Round up to nearest 3:
			int charsPerChannel = ((length + 2) / 3);
			int twoCharsPerChannel = charsPerChannel * 2;
			int startOffset = 0;

			if (charsPerChannel > 2)
			{
				// Check if all 3 start with a zero:

				for (int i = 0; i < charsPerChannel; i++)
				{

					if (randomString[i] == '0' && randomString[i + charsPerChannel] == '0' && randomString[i + twoCharsPerChannel] == '0')
					{

						// Removing the leading 0's.
						startOffset++;

					}

				}

			}

			// Load the RGB values now:
			int r = LoadHexFromPart(randomString, startOffset, charsPerChannel);
			int g = LoadHexFromPart(randomString, startOffset + charsPerChannel, twoCharsPerChannel);
			int b = LoadHexFromPart(randomString, startOffset + twoCharsPerChannel, length);

			return new Color(r / 255f, g / 255f, b / 255f, 1f);
		}

		private static string[] Colors=new string[]{
			"AliceBlue","F0F8FF",
			"AntiqueWhite","FAEBD7",
			"Aqua","00FFFF",
			"Aquamarine","7FFFD4",
			"Azure","F0FFFF",
			"Beige","F5F5DC",
			"Bisque","FFE4C4",
			"Black","000000",
			"BlanchedAlmond","FFEBCD",
			"Blue","0000FF",
			"BlueViolet","8A2BE2",
			"Brown","A52A2A",
			"BurlyWood","DEB887",
			"CadetBlue","5F9EA0",
			"Chartreuse","7FFF00",
			"Chocolate","D2691E",
			"Coral","FF7F50",
			"CornflowerBlue","6495ED",
			"Cornsilk","FFF8DC",
			"Crimson","DC143C",
			"Cyan","00FFFF",
			"DarkBlue","00008B",
			"DarkCyan","008B8B",
			"DarkGoldenRod","B8860B",
			"DarkGray","A9A9A9",
			"DarkGrey","A9A9A9",
			"DarkGreen","006400",
			"DarkKhaki","BDB76B",
			"DarkMagenta","8B008B",
			"DarkOliveGreen","556B2F",
			"DarkOrange","FF8C00",
			"DarkOrchid","9932CC",
			"DarkRed","8B0000",
			"DarkSalmon","E9967A",
			"DarkSeaGreen","8FBC8F",
			"DarkSlateBlue","483D8B",
			"DarkSlateGray","2F4F4F",
			"DarkSlateGrey","2F4F4F",
			"DarkTurquoise","00CED1",
			"DarkViolet","9400D3",
			"DeepPink","FF1493",
			"DeepSkyBlue","00BFFF",
			"DimGray","696969",
			"DimGrey","696969",
			"DodgerBlue","1E90FF",
			"FireBrick","B22222",
			"FloralWhite","FFFAF0",
			"ForestGreen","228B22",
			"Fuchsia","FF00FF",
			"Gainsboro","DCDCDC",
			"GhostWhite","F8F8FF",
			"Gold","FFD700",
			"GoldenRod","DAA520",
			"Gray","808080",
			"Green","008000",
			"GreenYellow","ADFF2F",
			"Grey","808080",
			"HoneyDew","F0FFF0",
			"HotPink","FF69B4",
			"IndianRed","CD5C5C",
			"Indigo","4B0082",
			"Ivory","FFFFF0",
			"Khaki","F0E68C",
			"Lavender","E6E6FA",
			"LavenderBlush","FFF0F5",
			"LawnGreen","7CFC00",
			"LemonChiffon","FFFACD",
			"LightBlue","ADD8E6",
			"LightCoral","F08080",
			"LightCyan","E0FFFF",
			"LightGoldenRodYellow","FAFAD2",
			"LightGray","D3D3D3",
			"LightGreen","90EE90",
			"LightGrey","D3D3D3",
			"LightPink","FFB6C1",
			"LightSalmon","FFA07A",
			"LightSeaGreen","20B2AA",
			"LightSkyBlue","87CEFA",
			"LightSlateGray","778899",
			"LightSlateGrey","778899",
			"LightSteelBlue","B0C4DE",
			"LightYellow","FFFFE0",
			"Lime","00FF00",
			"LimeGreen","32CD32",
			"Linen","FAF0E6",
			"Magenta","FF00FF",
			"Maroon","800000",
			"MediumAquaMarine","66CDAA",
			"MediumBlue","0000CD",
			"MediumOrchid","BA55D3",
			"MediumPurple","9370DB",
			"MediumSeaGreen","3CB371",
			"MediumSlateBlue","7B68EE",
			"MediumSpringGreen","00FA9A",
			"MediumTurquoise","48D1CC",
			"MediumVioletRed","C71585",
			"MidnightBlue","191970",
			"MintCream","F5FFFA",
			"MistyRose","FFE4E1",
			"Moccasin","FFE4B5",
			"NavajoWhite","FFDEAD",
			"Navy","000080",
			"OldLace","FDF5E6",
			"Olive","808000",
			"OliveDrab","6B8E23",
			"Orange","FFA500",
			"OrangeRed","FF4500",
			"Orchid","DA70D6",
			"PaleGoldenRod","EEE8AA",
			"PaleGreen","98FB98",
			"PaleTurquoise","AFEEEE",
			"PaleVioletRed","DB7093",
			"PapayaWhip","FFEFD5",
			"PeachPuff","FFDAB9",
			"Peru","CD853F",
			"Pink","FFC0CB",
			"Plum","DDA0DD",
			"PowderBlue","B0E0E6",
			"Purple","800080",
			"RebeccaPurple","663399",
			"Red","FF0000",
			"RosyBrown","BC8F8F",
			"RoyalBlue","4169E1",
			"SaddleBrown","8B4513",
			"Salmon","FA8072",
			"SandyBrown","F4A460",
			"SeaGreen","2E8B57",
			"SeaShell","FFF5EE",
			"Sienna","A0522D",
			"Silver","C0C0C0",
			"SkyBlue","87CEEB",
			"SlateBlue","6A5ACD",
			"SlateGray","708090",
			"SlateGrey","708090",
			"Snow","FFFAFA",
			"SpringGreen","00FF7F",
			"SteelBlue","4682B4",
			"Tan","D2B48C",
			"Teal","008080",
			"Thistle","D8BFD8",
			"Tomato","FF6347",
			"Transparent","00000000",
			"Turquoise","40E0D0",
			"Violet","EE82EE",
			"Wheat","F5DEB3",
			"White","FFFFFF",
			"WhiteSmoke","F5F5F5",
			"Yellow","FFFF00",
			"YellowGreen","9ACD32",

			// CSS2 System Colours
			"Activeborder","FFFFFF",
			"Activecaption","CCCCCC",
			"Appworkspace","FFFFFF",
			"Background","6363CE",
			"Buttonface","DDDDDD",
			"Buttonhighlight","DDDDDD",
			"Buttonshadow","888888",
			"Buttontext","000000",
			"Captiontext","000000",
			"Graytext","808080",
			"Highlight","B5D5FF",
			"Highlighttext","000000",
			"Inactiveborder","FFFFFF",
			"Inactivecaption","FFFFFF",
			"Inactivecaptiontext","7F7F7F",
			"Infobackground","FBFCC5",
			"Infotext","000000",
			"Menu","F7F7F7",
			"Menutext","000000",
			"Scrollbar","FFFFFF",
			"Threeddarkshadow","666666",
			"Threedface","FFFFFF",
			"Threedhighlight","DDDDDD",
			"Threedlightshadow","C0C0C0",
			"Threedshadow","888888",
			"Window","FFFFFF",
			"Windowframe","CCCCCC",
			"Windowtext","000000"
		};
	}
}
