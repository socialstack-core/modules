import omit from 'UI/Functions/Omit';

/**
 * A 12 segment responsive column. Usually used within a <Row>.
 */

export default class Column extends React.Component {
	
	render(){
		var props=this.props;
		return <div
			className={"col-md-" + (props.size || 6) + " " + (props.noGutters ? "no-gutters" : "") + (props.className ? ' ' + props.className : '')}
			{...(omit(this.props, ['className', 'noGutters', 'children', '__canvas']))}
		>
			{props.children}
		</div>;
	}
	
}

Column.propTypes={
	noGutters: 'boolean',
	size: [1,2,3,4,5,6,7,8,9,10,11,12],
	children: true
};

Column.icon = 'columns';