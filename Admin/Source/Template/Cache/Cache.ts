// ========================
// API Imports
// ========================
import templateApi, { Template } from "Api/Template";

// ========================
// Module-level Cache
// ========================
// Stores previously retrieved templates by their lowercase key.
// Helps reduce redundant API calls while still allowing fresh fetches.
let _cache: Map<string, Template> | undefined;

/**
 * getTemplateByKey
 *
 * Retrieves a `Template` by its key.
 * Uses a local cache to return immediately if previously fetched,
 * but still performs a fresh API call to ensure up-to-date data.
 *
 * Features:
 * - Caches templates in a `Map` keyed by their lowercase template key.
 * - If a template is already cached, the callback is executed immediately.
 * - Always performs a fresh API request to guarantee current data.
 * - Updates the cache with the latest result.
 *
 * Example:
 * ```
 * getTemplateByKey("site_default", (template) => {
 *     console.log("Template loaded:", template);
 * });
 * ```
 *
 * @param key       The template key (case-insensitive).
 * @param callback  Function to execute with the retrieved template.
 * @returns         A promise that resolves to the latest `Template` instance.
 */
const getTemplateByKey = (
	key: string,
	callback: (template: Template | null) => void
): Promise<Template | null> => {
	if (!_cache) {
		_cache = new Map<string, Template>();
	}

	const lcKey = (key || "").toLowerCase();
	const alreadyInCache = _cache.get(lcKey);

	// If found in cache, return it immediately via callback
	if (alreadyInCache) {
		// Clear error state:
		callback(alreadyInCache);
	}

	// Always fetch fresh from API (ensures cache never goes stale)
	return templateApi.getByKey(key).then((template: Template) => {
		// Update cache
		_cache!.set(template.key!.toLowerCase(), template);

		// Execute callback with latest data
		callback(template);

		return template;
	}).catch(e => {
		callback(null);
		return null;
	});
};

export { getTemplateByKey };
