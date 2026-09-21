import { useEditor } from 'UI/TinyMce';

/**
 * Props for the spacer component.
 * @icon fal fa-arrows-alt-v
 * @description An invisible space of a specified height.
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
	const { isEditing } = useEditor();

	if (hidden) {
		return;
	}

	if (!height) {
		height = 20;
	}

	return <div className="spacer-container">
		<div className={`spacer${isEditing ? ' spacer-editor' : ''}`} style={{ height: `${height}px` }}>
			{isEditing && <span>{`Spacer ${height}px`}</span>}
		</div>
	</div>;
}

export default Spacer;