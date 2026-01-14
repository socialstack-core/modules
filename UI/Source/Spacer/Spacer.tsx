/**
 * Props for the spacer component.
 */
interface SpacerProps {
	/**
	 * Height in pixels.
	 */
	height?: number,

	/**
	 * True if this spacer should be hidden.
	 */
	hidden?: boolean
}

/**
Just an invisible space of a specified height. The default is 20px.
*/
const Spacer: React.FC<SpacerProps> = props => {
	let { height, hidden } = props;

	if (hidden) {
		return;
	}

	if (!height) {
		height = 20;
	}

	return <div className="spacer-container">
		<div className="spacer" style={{ height: `${height}px` }}></div>
	</div>;
}

export default Spacer;