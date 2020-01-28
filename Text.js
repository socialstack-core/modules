// import translations from '@module/translations';

/**
 * This component displays translated text. Usage:
 * <Text>Hello world</Text>
 * <Text group='header'>Hello world</Text>
 * <Text group='header' key='hello_world'>Hello world</Text>
 */
export default class Text extends React.Component {

    constructor(props) {
        super(props);
        this.state = { translation: '' };
		this.reload(props);
    }
	
	componentWillReceiveProps(props){
		this.reload(props);
	}
	
	reload(props){
		var trans = props.children; // translations.get(props.children, props.group, props.replace);
		this.setState({translation: trans});
	}
	
	onClick(e){
		if(!e.target || !this.props.onClick){
			return;
		}
		this.props.onClick(e);
	}
	
    render() {
		if(this.state.translation){
			 return (<span onClick={e => this.onClick(e)} className={this.props.className} dangerouslySetInnerHTML={{__html: (this.state.translation)}} />);
		}
        return (
            <span onClick={e => this.onClick(e)} className={this.props.className}>{this.props.children}</span>
        );
    }
}

