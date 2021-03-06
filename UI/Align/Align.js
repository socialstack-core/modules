import omit from 'UI/Functions/Omit';

export default Align = props => <div style={{textAlign: props.type}} {...(omit(props, ['type', 'children']))}>
	{props.children}
</div>

Align.propTypes = {
	type: ['left', 'right', 'center', 'justify'],
	children: true
};
Align.icon = 'align-center';
