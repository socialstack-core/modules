import omit from 'UI/Functions/Omit';

export default class Align extends React.Component {
    constructor(props) {
        super(props);
    }
	
    render() {
		return <div style={{textAlign: this.props.type}} {...(omit(this.props, ['type', 'children']))}>{this.props.children}</div>;
    }
}

Align.propTypes = {
	type: ['left', 'right', 'center', 'justify'],
	children: true
};
Align.icon = 'align-center';
