var store = window.localStorage;

/**
 * Gets the given object by textual key name from the store.
 * @param key
 * @returns
 */
function get<T>(key: string) {
	if (!store) {
		return null;
	}

	const val = store.getItem(key);
	if (!val) {
		return null;
	}

	try {
		return JSON.parse(val) as T;
	} catch (e) {
		console.error(`Invalid JSON for key "${key}". Clearing value.`, e);
		remove(key);
		return null;
	}
}


/**
 * Sets the given value in to the store by JSON stringifying it.
 * @param key
 * @param value
 */
function set(key : string, value : any){
	store.setItem(key, JSON.stringify(value));
}

/**
 * Removes the given key from the store.
 * @param key
 */
function remove(key : string){
	store.removeItem(key);
}

export default {get, set, remove};