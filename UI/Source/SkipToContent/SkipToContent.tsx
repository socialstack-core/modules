/**
 * Props for the SkipToContent component.
 */
interface SkipToContentProps extends React.HTMLAttributes<HTMLAnchorElement> {
	/**
	 * horizontal alignments for the skip link (defaults to left)
	 */
	horizontalAlignment?: "left" | "start" | "center" | "centre" | "middle" | "right" | "end" | undefined;

	/**
	 * link target (defaults to "#site-content")
	 */
	linkTarget?: string;
}

/**
 * Skip to main content component.
 */

const SkipToContent: React.FC<React.PropsWithChildren<SkipToContentProps>> = ({ children, horizontalAlignment, linkTarget, ...props }) => {
	let skipClasses = ['ui-skip-to-content'];
	let target = linkTarget || `#site-content`;

	switch (horizontalAlignment) {
		case 'center':
		case 'centre':
		case 'middle':
			skipClasses.push('ui-skip-to-content--top-centre');
			break;

		case 'right':
		case 'end':
			skipClasses.push('ui-skip-to-content--top-right');
			break;

		default:
			skipClasses.push('ui-skip-to-content--top-left');
			break;
	}

	return <>
		{/* hide skip to content link if no content to skip or target does not exist  */}
		<style>
			{`.ui-skip-to-content:has(+ ${target}){display:none}`}
			{`#react-root:not(:has(${target})){.ui-skip-to-content{display:none}}`}
		</style>
		<a href={target} className={skipClasses.join(' ')} {...props}>
			{children}
			{/* set default link content if nothing supplied */}
			{!children && <>
				{`Skip to main content`}
			</>}
		</a>
	</>;

}

export default SkipToContent;