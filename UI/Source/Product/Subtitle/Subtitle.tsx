/**
 * Props for the Subtitle component.
 */
interface SubtitleProps {
	/**
	 * text
	 */
	subtitle?: string,
}

/**
 * The Subtitle React component.
 * @param props React props.
 */
const Subtitle: React.FC<SubtitleProps> = (props) => {
	const { subtitle } = props;

	if (!subtitle?.length) {
		return;
	}

	return <>
		<h2 className="ui-product-view__subtitle">
			{subtitle}
		</h2>
	</>;
}

export default Subtitle;