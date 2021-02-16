import omit from 'UI/Functions/Omit';

export default Align = () => {
    render() {
		return <div style={{textAlign: props.type}} {...(omit(props, ['type', 'children']))}>{props.children}</div>;
    }
}

Align.propTypes = {
	type: ['left', 'right', 'center', 'justify'],
	children: true
};
Align.icon = 'align-center';
