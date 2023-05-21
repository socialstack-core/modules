import omit from 'UI/Functions/Omit';

/**
 * A specific width container for content (maps directly to bootstrap 'container' by default).
 * Can also use type="sm", "md", "lg", "xl", "fluid" - see also: bootstrap's container names.
 */
export default function container(props) {
	let { hidden } = props;

	if (hidden) {
		return;
	}

	return <div className={"container" + (props.type ? "-" + props.type : '') + " " + (props.className || '')} {...omit(props, ['type', 'children', 'className'])}>
		{props.children}
	</div>;
}


container.propTypes = {
	className: 'string',
	type: ['','sm','md','lg','xl'],
	children: true,
	hidden: 'bool'
};

container.icon = 'cube';