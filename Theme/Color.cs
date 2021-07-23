using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Api.Themes
{
	
	/// <summary>
	/// Used to map colour name to colour value.
	/// </summary>
	
	public struct Color
	{
		/// <summary>
		/// Black
		/// </summary>
		public static Color Black = new Color(0, 0, 0, 1);

		/// <summary>
		/// White
		/// </summary>
		public static Color White = new Color(1,1,1, 1);


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public Color GetContrastColor()
		{
			// http://www.w3.org/TR/AERT#color-contrast
			var brightness = Math.Round(
					((R * 0.299f) +
					(G * 0.587f) +
					(B * 0.114f)) * 255f
				);

			return (brightness > 125f) ? Color.Black : Color.White;
		}


		/// <summary>
		/// HSL->RGB color. All hsl values must be normalised to 0-1.
		/// </summary>
		/// <param name="h"></param>
		/// <param name="s"></param>
		/// <param name="l"></param>
		/// <param name="a"></param>
		/// <returns></returns>
		public static Color FromHsl(float h, float s, float l, float a)
		{
			var m2 = (l <= 0.5f) ? l * (s + 1f) : l + s - l * s;
			var m1 = l * 2f - m2;

			var r = HueToRgb(m1, m2, h + 1f / 3f);
			var g = HueToRgb(m1, m2, h);
			var b = HueToRgb(m1, m2, h - 1f / 3f);
			return new Color(r, g, b, a);
		}

		private static float HueToRgb(float m1, float m2, float h)
		{
			if (h < 0f)
			{
				h++;
			}

			if (h > 1f)
			{
				h--;
			}

			if (h * 6f < 1f)
			{
				return m1 + (m2 - m1) * h * 6f;
			}

			if (h * 2f < 1f)
			{
				return m2;
			}

			if (h * 3f < 2f)
			{
				return m1 + (m2 - m1) * (2f / 3f - h) * 6f;
			}

			return m1;
		}
		

		/// <summary>
		/// Red
		/// </summary>
		public float R;
		
		/// <summary>
		/// Green
		/// </summary>
		public float G;
		
		/// <summary>
		/// Blue
		/// </summary>
		public float B;
		
		/// <summary>
		/// Alpha
		/// </summary>
		public float A;
		
		
		/// <summary>
		/// Creates a color initialised to the given rgb(a) values.
		/// </summary>
		public Color(float r, float g, float b, float a = 1f)
		{
			R = r;
			G = g;
			B = b;
			A = a;
		}

		/// <summary>
		/// Writes this color out as a CSS value.
		/// </summary>
		/// <returns></returns>
		public string ToCss()
		{
			var sb = new StringBuilder();
			ToCss(sb);
			return sb.ToString();
		}

		/// <summary>
		/// Writes this color out as a CSS value into the given builder.
		/// </summary>
		/// <param name="sb"></param>
		public void ToCss(StringBuilder sb)
		{
			if (A <= 0)
			{
				sb.Append("transparent");
				return;
			}

			byte r = (byte)(R * 255);
			byte g = (byte)(G * 255);
			byte b = (byte)(B * 255);

			if (A == 1)
			{
				// hex format is a bit more compact
				sb.Append('#');
				ToHex(r, sb);
				ToHex(g, sb);
				ToHex(b, sb);
			}
			else
			{
				// RGBA
				sb.Append("rgba(");
				sb.Append(r);
				sb.Append(',');
				sb.Append(g);
				sb.Append(',');
				sb.Append(b);
				sb.Append(',');
				sb.Append(A);
				sb.Append(')');
			}
		}

		private void ToHex(byte value, StringBuilder sb)
		{
			var upper = value >> 4;
			var lower = value & 15;

			sb.Append((char)(upper >= 10 ? 'A' + (upper-10) : '0' + upper));
			sb.Append((char)(lower >= 10 ? 'A' + (lower-10) : '0' + lower));
		}

		/// <summary>
		/// Gets this colour as HSL.
		/// </summary>
		/// <param name="h"></param>
		/// <param name="s"></param>
		/// <param name="l"></param>
		public void ToHsl(out float h, out float s, out float l)
		{
			float r = R;
			float g = G;
			float b = B;
			float v;
			float m;
			float vm;
			float r2, g2, b2;

			h = 0; // default to black
			s = 0;

			v = Math.Max(r, g);
			v = Math.Max(v, b);
			m = Math.Min(r, g);
			m = Math.Min(m, b);

			l = (m + v) / 2.0f;

			if (l <= 0.0f)
			{
				return;
			}

			vm = v - m;
			s = vm;

			if (s > 0.0f)
			{
				s /= (l <= 0.5f) ? (v + m) : (2.0f - v - m);
			}
			else
			{
				return;
			}

			r2 = (v - r) / vm;
			g2 = (v - g) / vm;
			b2 = (v - b) / vm;

			if (r == v)
			{
				h = (g == m ? 5.0f + b2 : 1.0f - g2);
			}
			else if (g == v)
			{
				h = (b == m ? 1.0f + r2 : 3.0f - b2);
			}
			else
			{
				h = (r == m ? 3.0f + g2 : 5.0f - r2);
			}

			h /= 6.0f;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="amount">0-1 scaled percentage</param>
		/// <returns></returns>
		public Color Lighten(float amount)
		{
			float  h, s, l;
			ToHsl(out h, out s, out l);
			l = Math.Min(l + amount, 1f);

			return FromHsl(h, s, l, A);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
		public Color DarkenColor(float amount)
		{
			float h, s, l;
			ToHsl(out h, out s, out l);
			l = Math.Max(l - amount, 0f);

			return FromHsl(h, s, l, A);
		}

		/// <summary>
		/// Mixes this color with the given one.
		/// </summary>
		/// <param name="color2"></param>
		/// <param name="weightScale"></param>
		/// <returns></returns>
		public Color Mix(Color color2, float weightScale)
		{
			// ref: https://fossies.org/linux/dart-sass/lib/src/functions/color.dart
			float normalizedWeight = weightScale * 2 - 1;
			var alphaDistance = A - color2.A;

			var combinedWeight1 = normalizedWeight * alphaDistance == -1
				? normalizedWeight
				: (normalizedWeight + alphaDistance) /
				(1 + normalizedWeight * alphaDistance);

			float weight1 = (combinedWeight1 + 1) / 2;
			float weight2 = 1 - weight1;

			var r = R * weight1 + color2.R * weight2;
			var g = G * weight1 + color2.G * weight2;
			var b = B * weight1 + color2.B * weight2;
			var a = A * weight1 + color2.A * weight2;
			
			return new Color(r,g,b,a);
		}

		private Color Mix2(Color b, float weight)
		{
			return new Color(
				R + ((b.R - R) * weight),
				G + ((b.G - G) * weight),
				B + ((b.B - B) * weight),
				A + ((b.A - A) * weight)
			);
		}

		/// <summary>
		/// True if this is white.
		/// </summary>
		public bool IsWhite
		{
			get {
				return R >= 1f && G >= 1f && B >= 1f && A>=1f;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="weight"></param>
		/// <returns></returns>
		public Color TintColor(float weight)
		{
			return Mix2(White, weight);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="weight"></param>
		/// <returns></returns>
		public Color ShadeColor(float weight)
		{
			return Mix2(Black, weight);
		}
		
	}

}