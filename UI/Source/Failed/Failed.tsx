import Icon from 'UI/Icon';

/** 
 * Throw this to create server compatible exception messages.
 */
export class PublicError extends Error {

	/**
	 * The error type. Usually a two part general/specific combination such as "invalid/json".
	 */
	type: string;
	/**
	 * A human friendly error message. Display this on the UI.
	 */
	message: string;
	/**
	 * The underlying error, if there was one.
	 */
	baseError?: Error;

	constructor(type: string, message: string, baseError?: Error) {
		super(type + ': ' + message);
		this.type = type;
		this.message = message;
		this.baseError = baseError;
		console.error(type, message, baseError);
	}

	toString() {
		if (this.baseError) {
			return this.baseError?.toString();
		}

		return this.type + ': ' + this.message;
	}
}

/**
 * Displays a generic error message.
 */
const Failed: React.FC<{}> = () => {
	return(
		<div className="alert alert-danger" role="alert" style={{ textAlign: "center" }}>
			<Icon type="fa-wifi-slash" duotone />
			<p>{`The service is currently unavailable. This may be because your device is currently offline.`}</p>
		</div>
	);
}

export default Failed;