import roleApi from 'Api/Role';

interface RoleCapabilities {
	loading?: Promise<RoleCapabilities>,
	capabilities: Map<string, boolean>
}

/* cache */
var roleCache : Map<int, RoleCapabilities> | null = null;

/**
* Returns a promise which resolves to true or false if the given named capability is granted or not.
*/
export default (capName: string, session: Session) => {
	if(session.loadingUser){
		return session.loadingUser.then(() => loadCached(capName, session));
	}
	
	return loadCached(capName, session);
}

/**
 * Loads a cached capability entry for a users role.
 * @param capName
 * @param session
 * @returns
 */
function loadCached(capName: string, session: Session) : Promise<boolean> {
	var roleId = session.user?.role || 0 as int;

	if (!roleCache) {
		roleCache = new Map<int, RoleCapabilities>();
	}

	var cachedRole = roleCache.get(roleId);

	if (!cachedRole) {
		var newRole: RoleCapabilities = {
			capabilities: new Map<string, boolean>()
		};

		newRole.loading = roleApi.load(roleId).then(role => {
			role.capabilities?.forEach(c => {
				cachedRole?.capabilities.set(c, true);
			});
			newRole.loading = undefined;
			return newRole;
		});

		cachedRole = newRole;
	}

	if (cachedRole.loading) {
		return cachedRole.loading.then(rc => !!rc.capabilities.get(capName));
	}

	return Promise.resolve(!!cachedRole.capabilities.get(capName));
}