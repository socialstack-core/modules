var store = window.localStorage;

export function get(key){
	var val = store.getItem(key);
	return val ? JSON.parse(val) : null;
}

export function set(key, value){
	store.setItem(key, JSON.stringify(value));
}

export function remove(key){
	store.removeItem(key);
}