
/**
 * Standalone component which displays a loader (typically a spinner).
 */
export default function Loading (props) {
	let message = props.message || `Loading ... `;
	
	return (
		<div className="alert alert-info loading">
			{message}
			<i className="fas fa-spinner fa-spin" />
		</div>
	);
}