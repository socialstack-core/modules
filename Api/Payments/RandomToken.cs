using System;
using System.Security.Cryptography;
using System.Text;

namespace Api.Payments
{
	/// <summary>
	/// Used for generating random strings.
	/// </summary>
	public static class RandomToken
	{
		
		/// <summary>
		/// Generate a random string using cryptographically-secure randomness.
		/// Optionally inserts '-' every <paramref name="segmentLength"/> characters.
		/// If <paramref name="segmentLength"/> &lt;= 0, no dashes are inserted.
		/// </summary>
		public static string Generate(int maxLength = 20, int segmentLength = 0, string pattern = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890")
		{
			if (maxLength <= 0) {
				throw new ArgumentException("maxLength must be > 0", nameof(maxLength));
			}

			if (segmentLength < 0) {
				throw new ArgumentException("segmentLength must be >= 0 (use 0 for no segmentation)", nameof(segmentLength));
			}

			if (string.IsNullOrEmpty(pattern)) {
				throw new ArgumentException("pattern must not be null or empty", nameof(pattern));
			}

			int dashCount = (segmentLength > 0) ? ( (maxLength - 1) / segmentLength ) : 0;
			int totalLength = maxLength + dashCount;
			var pat = pattern; 
			int patLen = pat.Length;

			return string.Create(totalLength, (maxLength, segmentLength, patLen, pat),
				(Span<char> span, (int max, int seg, int pLen, string p) state) =>
			{
				int pos = 0;

				if (state.seg <= 0)
				{
					// simpler loop when no dashes requested
					for (int i = 0; i < state.max; i++)
					{
						int idx = RandomNumberGenerator.GetInt32(state.pLen);
						span[pos++] = state.p[idx];
					}
				}
				else
				{
					// segmented version
					for (int i = 0; i < state.max; i++)
					{
						if (i > 0 && (i % state.seg) == 0)
						{
							span[pos++] = '-';
						}

						int idx = RandomNumberGenerator.GetInt32(state.pLen);
						span[pos++] = state.p[idx];
					}
				}
			});
		}
	}
}
