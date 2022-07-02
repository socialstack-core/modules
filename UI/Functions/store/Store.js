var store = window.localStorage;

function get(key){
	var val = store.getItem(key);
	return val ? JSON.parse(val) : null;
}

function set(key, value){
	store.setItem(key, JSON.stringify(value));
}

function remove(key){
	store.removeItem(key);
}

export default {get, set, remove};