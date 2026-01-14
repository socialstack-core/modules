import store from "UI/Functions/Store";


const addToRecentSearches = (text: string) => {

	if (!text) {
		return;
	}

	let current: string[] = store.get('recent_searches') ?? [];

	if (current.length > 9) {
		current.length = 9;
	}

	current = current.filter(entry => entry?.length && entry != text)

	current = [text, ...current];

	store.set('recent_searches', current);
}


const getSearchHistory = (): string[] => store.get('recent_searches') ?? [];

const removeItemFromSearchHistory = (text: string): string[] => {
	let current: string[] = store.get('recent_searches') ?? [];
	current = current.filter(entry => entry?.length && entry != text);
	store.set('recent_searches', current);
	return current;
} 

export {
	getSearchHistory, 
	addToRecentSearches,
	removeItemFromSearchHistory,
}