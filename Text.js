import omit from 'UI/Functions/Omit';

/**
 * This component displays translated text. Usage:
 * <Text>Hello world</Text>
 * <Text group='header'>Hello world</Text>
 * <Text group='header' key='hello_world'>Hello world</Text>
 */
export default class Text extends React.Component {
    constructor(props) {
        super(props);
    }
	
    render() {
		var text = <span 
			dangerouslySetInnerHTML={{__html: (this.props.text || this.props.children)}}
			{...omit(this.props, ['text', 'children'])}
		/>;
		
		const {align} = this.props;
		
		if(align && align != 'none'){
			// Block display if align is used
			return <div style={{textAlign: align}}>{text}</div>;
		}
		
		return text;
    }
}

Text.propTypes = {};
Text.icon = 'align-justify';

Text.rendererPropTypes = {
	text: 'string',
	align: ['none', 'left', 'right', 'center', 'justify']
};
