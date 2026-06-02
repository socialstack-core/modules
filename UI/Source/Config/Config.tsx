/**
 * Returns localised server set config
 * @param name
 * @param locale
 * @param maxRecords max number of instances of this config item (optional, defaults to 1)
 * @returns
 */
export function getLocalisedConfig<T>(name: string, localeCode?: string, maxRecords?: int): T | null {
	//const { session } = useSession();
	//var { locale } = session;

	var config = getConfig(name) as T[];

	if (!config || !config.length) {
		return null;
	}

	if (config.length > (maxRecords || 1)) {
		console.warn(`expected one ${name} config (found ${config.length} entries)`);
	}

	var entry = config[0] as T;

	// ensure locale is fully qualified
	if (!localeCode || localeCode === "en") {
		localeCode = "en-gb";
	}

	if ((entry as any).localised && localeCode?.length) {

		for (let [key, value] of Object.entries((entry as any).localised as Record<string, any>)) {

			if (!value) {
				continue;
			}

			var field = value[localeCode] as string;

			if (field?.length) {
				(entry as any)[key] = field;
			}
		}

		delete (entry as any).localised;
	}

	return entry;
}

/**
 * Obtains server set config with the given case insensitive name.
 * @param name
 * @returns
 */
export default function getConfig<T>(name: string): T[] | null {
	var cfg = window.__cfg ? window.__cfg[name.toLowerCase()] : null;

	return cfg as T[]
}