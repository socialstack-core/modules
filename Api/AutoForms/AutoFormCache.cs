using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;



namespace Api.AutoForms
{
	
	/// <summary>
	/// A cache for a particular type of autoform.
	/// </summary>
	public class AutoFormCache
	{
		
		/// <summary>
		/// Called when populating the cache.
		/// </summary>
		private Func<Context, Dictionary<string, AutoFormInfo>, ValueTask> populate;
		/// <summary>
		/// The role service.
		/// </summary>
		private RoleService _roleService;

		/// <summary>
		/// Creates a new empty cache with the given population function.
		/// </summary>
		/// <param name="onPopulate"></param>
		/// <param name="roleService"></param>
		public AutoFormCache(Func<Context, Dictionary<string, AutoFormInfo>, ValueTask> onPopulate, RoleService roleService)
		{
			populate = onPopulate;
			_roleService = roleService;
		}
	
		private Dictionary<string, AutoFormInfo>[] _cachedAutoFormInfo = null;
		
		/// <summary>
		/// Clears this cache.
		/// </summary>
		public void Clear()
		{
			_cachedAutoFormInfo = null;
		}

		private object createLock = new object();

		/// <summary>
		/// Gets role specific autoform fields.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public async ValueTask<Dictionary<string, AutoFormInfo>> GetForRole(Context context)
		{
			var roleId = context.RoleId;
			var role = await _roleService.Get(context, roleId);

			if (role == null)
			{
				// Bad role ID.
				return null;
			}

			if (_cachedAutoFormInfo == null || _cachedAutoFormInfo.Length < role.Id)
			{
				lock (createLock)
				{
					if (_cachedAutoFormInfo == null)
					{
						_cachedAutoFormInfo = new Dictionary<string, AutoFormInfo>[role.Id];
					}
					else if (_cachedAutoFormInfo.Length < role.Id)
					{
						// Resize it:
						Array.Resize(ref _cachedAutoFormInfo, (int)role.Id);
					}
				}
			}
			
			var cachedSet = _cachedAutoFormInfo[roleId - 1];

			if (cachedSet != null)
			{
				return cachedSet;
			}

			var result = new Dictionary<string, AutoFormInfo>();
			
			// Run pop func:
			await populate(context, result);

			lock (createLock)
			{
				// Multiple threads can run populate on new objects (incl for the same role)
				// but it's crucial that only 1 thread at a time actually puts entries in to the array.
				_cachedAutoFormInfo[role.Id - 1] = result;
			}

			return result;
		}
		
	}
	
}