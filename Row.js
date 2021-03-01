import omit from 'UI/Functions/Omit';

/**
 * A responsive row.
 */

export default function Row(props) {
	var rowClass = "row ";

	if (props.noGutters) {
		rowClass += "no-gutters ";
	}

	if (props.horizontalAlignment) {
		rowClass += "justify-content-" + props.horizontalAlignment + " ";
	}

	if (props.className) {
		rowClass += props.className;
	}

	return <div 
		className={rowClass}
		{...(omit(props, ['className', 'noGutters', 'children', '__canvas']))}
	>
		{props.children}
	</div>;
}

Row.propTypes={
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