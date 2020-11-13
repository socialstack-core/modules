/**
 * This component displays html. Only ever use this with trusted text.
*/

export default class Html extends React.Component {

    constructor(props) {
        super(props);
    }
	
    render() {
		var ch = this.props.children;
		if(Array.isArray(ch)){
			ch = ch.length ? ch[0] : "";
		}
        return <span {...this.props} dangerouslySetInnerHTML={{__html: ch}} />;
    }
}

