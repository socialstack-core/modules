/**
 * Props for the Html component.
 */
interface HtmlProps extends React.HTMLAttributes<HTMLSpanElement> {
	/**
	 * The HTML wrapping tag to use (defaults to span if not supplied)
	 */
	tag?: string;

    /**
     * The HTML to display.
     */
	content?: string;
}

/**
 * This component displays html. Only ever use this with trusted text.
*/
const Html: React.FC<HtmlProps> = ({ tag, content, ...props }) => {

	if (!content?.length) {
		content = props.children;
	}

	if (!content?.length) {
		return;
	}

	const Tag = !tag?.length ? "span" : tag;

    return <Tag dangerouslySetInnerHTML={{ __html: content }} {...props} />;
}

export default Html;