
using Microsoft.AspNetCore.Http;
using System;

namespace Api.Payments
{
	/// <summary>
	/// Helper class for httpcontext requests 
	/// </summary>
	public static class RequestHelper
	{
		/// <summary>
		/// Get the current requests ip address
		/// </summary>
		/// <returns></returns>
		public static string GetClientIp(HttpContext ctx)
		{

			if (ctx == null)
			{
				return null;
			}

			// Apply your precedence logic (CF-Connecting-IP, True-Client-IP, CloudFront-Viewer-Address, X-Real-IP, XFF, Forwarded, RemoteIp)
			var cf = ctx.Request.Headers["CF-Connecting-IP"].ToString();
			if (!string.IsNullOrWhiteSpace(cf))
			{
				return cf;
			}

			var tci = ctx.Request.Headers["True-Client-IP"].ToString();
			if (!string.IsNullOrWhiteSpace(tci))
			{
				return tci;
			}

			var cfViewerAddr = ctx.Request.Headers["CloudFront-Viewer-Address"].ToString();
			if (!string.IsNullOrWhiteSpace(cfViewerAddr))
			{
				var ipPart = cfViewerAddr.Split(':')[0].Trim();
				if (!string.IsNullOrWhiteSpace(ipPart))
				{
					return ipPart;
				}
			}

			var xReal = ctx.Request.Headers["X-Real-IP"].ToString();
			if (!string.IsNullOrWhiteSpace(xReal)) return xReal;

			var xff = ctx.Request.Headers["X-Forwarded-For"].ToString();
			if (!string.IsNullOrWhiteSpace(xff))
			{
				var first = xff.Split(',')[0].Trim();
				if (!string.IsNullOrWhiteSpace(first))
				{
					return first;
				}
			}

			var fwd = ctx.Request.Headers["Forwarded"].ToString();
			if (!string.IsNullOrWhiteSpace(fwd))
			{
				foreach (var part in fwd.Split(';', ','))
				{
					var kv = part.Split('=');
					if (kv.Length == 2 && kv[0].Trim().Equals("for", StringComparison.OrdinalIgnoreCase))
					{
						var ip = kv[1].Trim().Trim('"');
						if (!string.IsNullOrWhiteSpace(ip))
						{
							return ip;
						}
					}
				}
			}

			return ctx.Connection.RemoteIpAddress?.ToString();

		}
	}
}