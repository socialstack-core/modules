/**
 * Returns localised server set config
 * @param name
 * @param locale
 * @param maxRecords max number of instances of this config item (optional, defaults to 1)
 * @returns
 */
export function getLocalisedConfig<T>(name: string, locale: string, maxRecords?: int): T | null {
	//const { session } = useSession();
	//var { locale } = session;

	var config = getConfig(name);

	if (!config || !config.length) {
		return null;
	}

	if (config.length > (maxRecords || 1)) {
		console.warn(`expected one ${name} config (found ${config.length} entries)`);
	}

	config = config[0];

	var localeCode = locale?.code;

	// ensure locale is fully qualified
	if (localeCode === "en") {
		localeCode = "en-gb";
	}

	if (config.localised && localeCode?.length) {

		for (let [key, value] of Object.entries(config.localised)) {

			if (!value) {
				continue;
			}

			if (value[localeCode]?.length) {
				config[key] = value[localeCode];
			}
		}

		delete config.localised;
	}

	return config as T;
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