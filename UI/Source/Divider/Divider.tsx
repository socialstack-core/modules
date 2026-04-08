/**
 * Props for the Divider component.
 */
interface DividerProps {
}

/**
 * The Divider React component.
 * @param props React props.
 */
const Divider: React.FC<DividerProps> = (props) => {
	return (
		<hr className="ui-divider" />
	);
}

export default Divider;