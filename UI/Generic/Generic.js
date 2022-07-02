export default function Generic(props) {
	const { name, children } = props;

	return React.createElement(name, {...props, name: null} , children)
}


Generic.propTypes = {
	name: 'string',
	classname: 'string'
};

// use defaultProps to define default values, if required
Generic.defaultProps = {
	name: 'generic'
}

// icon used to represent component when adding to a page via /en-admin/page
// see https://fontawesome.com/icons?d=gallery for available classnames
// NB: exclude the "fa-" prefix
Generic.icon='puzzle-piece'; // fontawesome icon
