import omit from 'UI/Functions/Omit';

/**
 * A responsive row.
 */

export default class Row extends React.Component {
	
	render(){
		var props=this.props;
		return <div 
			className={"row " + (props.noGutters ? "no-gutters" : "") + " " + (props.className || '')}
			{...(omit(this.props, ['className', 'noGutters', 'children', '__canvas']))}
		>
			{props.children}
		</div>;
	}

}

Row.propTypes={
	noGutters: 'boolean',
	children: {default: [{module: "UI/Column", content: "Column 1"}, {module: "UI/Column", content: "Column 2"}]}
};

Row.icon = 'columns';