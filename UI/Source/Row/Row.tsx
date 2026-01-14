/**
 * Props for the Column component.
 */
interface RowProps extends React.HTMLAttributes<HTMLDivElement> {

	/**
	 * Horizontal alignments for the row.
	 */
	horizontalAlignment?: "start" | "end" | "center" | "between" | "around" | "evenly";

	/**
	 * True if the column order is reversed on mobile.
	 */
	reverseColumnOrderOnMobile?: boolean;

	/**
	 * Custom class name(s).
	 */
	className?: string;

	/**
		Set this to true if you don't want gutters on your row.
	*/
	noGutters?: boolean;

	children?: React.ReactNode | null
}

/**
 * A responsive row.
 */

const Row: React.FC<React.PropsWithChildren<RowProps>> = ({ children, horizontalAlignment, reverseColumnOrderOnMobile, className, noGutters, ...props }) => {
	var rowClass = "row ";

	if (noGutters) {
		rowClass += "gx-0 ";
	}

	if (horizontalAlignment) {
		rowClass += "justify-content-" + horizontalAlignment + " ";
	}

	if (className) {
		rowClass += className;
	}

	if (reverseColumnOrderOnMobile) {
		rowClass += " row--reverse-mobile-columns";
	}

	return <div 
		className={rowClass}
		{...props}
	>
		{children}
	</div>;
}

export default Row;

/*
Row.propTypes={
	reverseColumnOrderOnMobile: 'boolean',
	noGutters: 'boolean',
	horizontalAlignment: [
		{ name: 'No preference', value: '' },
		{ name: 'Align left', value: 'start' },
		{ name: 'Align centre', value: 'center' },
		{ name: 'Align right', value: 'end' },
		{ name: 'Distribute space around', value: 'around' },
		{ name: 'Distribute space between', value: 'between' }
	],
	children: {default: [{module: "UI/Column", content: "Column 1"}, {module: "UI/Column", content: "Column 2"}]}
};

Row.icon = 'columns';
*/
