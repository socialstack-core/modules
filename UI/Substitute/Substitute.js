
/**
 * This component will substitute itself with something known by the canvas by calling onSubstitute on the canvas itself.
 */
export default function Substitute (props) {
	var canvas = props.__canvas;
	return canvas && canvas.props.onSubstitute && canvas.props.onSubstitute(props.name) || null;
}

Substitute.propTypes={
	name: 'string'
}