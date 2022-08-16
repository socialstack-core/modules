export default function Emoji(props) {
	const { label, symbol } = props;

	var className = props.className ? "emoji " + props.className : "emoji";

	return (
		<span className={className} role="img" aria-label={label}>
			{String.fromCodePoint(symbol)}
		</span>
	);
}
