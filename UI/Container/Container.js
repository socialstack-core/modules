import omit from 'UI/Functions/Omit';

/**
 * A specific width container for content (maps directly to bootstrap 'container' by default).
 * Can also use type="sm", "md", "lg", "xl", "fluid" - see also: bootstrap's container names.
 */
const container = (props) =>
	<div className={"container" + (props.type ? "-" + props.type : '') + " " + (props.className || '')} {...omit(props, ['type', 'children', 'className'])}>
		{props.children}
	</div>;

export default container;

container.propTypes = {
	className: 'string',
	type: ['','sm','md','lg','xl'],
	children: true
};
container.icon = 'cube';