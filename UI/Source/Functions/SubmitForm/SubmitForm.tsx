/**
 * Used to construct a form field set and returns it in a promise.
 * If the fields were unable to be constructed because they contain validation errors 
 * then the promise will reject with a PublicError stating the validation failure itself.
 * @param e
 * @returns A promise which is rejected if any of the fields have validation errors.
 */
const SubmitForm = (e: React.FormEvent<HTMLFormElement>) => {
	e.preventDefault();
	
	var fields = (e.target as HTMLFormElement)?.elements;
	var values: Record<string, any> = {};
	var valuePromises = [];
	
	for (var i = 0; i < fields.length; i++) {
		valuePromises.push(collectFieldValue(fields[i], values));
	}

	var submitterName: string = (e as any).submitter?.name;

	if(submitterName){
		values[submitterName] = (e as any).submitter.value;
	}
	
	return Promise.all(valuePromises)
		.then(() => values);
}

/**
 * Collects the given form field's value, putting it in to the given values set.
 * Handles a custom field function called onGetValue such that a form can contain types beyond strings only.
 * @param field
 * @param values
 * @returns A promise which is rejected if any of the fields have validation errors.
 */
const collectFieldValue = (field: any, values: Record<string, any>): Promise<void> => {
	
	return new Promise((resolve, reject) => {
		
		if (!field.name || field.type === 'submit' || (field.type === 'radio' && !field.checked)) {
			// Not a field we care about.
			resolve();
			return;
		}

		const name: string = field.name;
		let value = field.type === 'checkbox' ? field.checked : field.value;

		if (field.onGetValue) {
			try {
				value = field.onGetValue(value, field);
			} catch (err) {
				reject(err);
				return;
			}

			const ogvPromise = value && typeof value.then === 'function'
				? value
				: Promise.resolve(value);

			ogvPromise
				.then((val: any) => {
					field.value = val;
					values[name] = val;

					if (field.onValidationCheck) {
						const valError = field.onValidationCheck(field) as PublicError;
						if (valError) {
							return reject(valError);
						}
					}

					resolve();
				})
				.catch(reject);
		} else {
			values[name] = value;

			// Final validation
			if (field.onValidationCheck) {
				const valError = field.onValidationCheck(field) as PublicError;
				if (valError) {
					reject(valError);
					return;
				}
			}

			resolve();
		}
	});
};


export default SubmitForm;
