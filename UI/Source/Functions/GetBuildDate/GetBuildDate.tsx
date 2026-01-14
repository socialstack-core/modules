var buildDate: BuildDate | null = null;

interface BuildDate {
	/**
	 * Timestamp converted to UTC date.
	 */
	date: Date,
	/** 
	 * The date as an ISO string.
	 */
	dateString: string,
	/**
	 * The original UTC timestamp.
	 */
	timestamp: int
}

/*
* Reads the build date whenever it's available.
* It originates from the main.generated?v=X number (where X is actually a UTC unix timestamp in ms)
*/
export default () : BuildDate => {
	if (!buildDate) {
		var timestampStr = document.body.getAttribute("data-ts");
		var timestamp = parseInt(timestampStr || '') as int;
		var date = new Date(timestamp);

		buildDate = {
			date,
			dateString: date.toUTCString(),
			timestamp
		};
	}
	
	return buildDate;
};