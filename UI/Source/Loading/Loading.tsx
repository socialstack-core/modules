/**
 * Props for the Loading component.
 */
interface LoadingProps extends React.HTMLAttributes<HTMLSpanElement> {
	/**
	 * Render small version of loading spinner.
	 */
	small?: boolean,

	/**
	 * Optionally change the message being displayed.
	 */
	message?: string
}

/**
 * Standalone component which displays a loader (typically a spinner).
*/
const Loading: React.FC<LoadingProps> = (props) => {
	let message = props.message || `Loading ...`;
	
	return (
		<div className={props.small ? "loading loading--small" : "loading"}>
			<svg className="loading__spinner" viewBox="0 0 66 66" xmlns="http://www.w3.org/2000/svg">
				<circle fill="none" stroke-width="6" stroke-linecap="round" cx="33" cy="33" r="30"></circle>
			</svg>
			<span className="loading__text">
				{message}
			</span>
		</div>
	);
}

export default Loading;