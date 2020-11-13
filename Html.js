/**
 * This component displays html. Only ever use this with trusted text.
*/

export default class Html extends React.Component {

    constructor(props) {
        super(props);
    }
	
    render() {
		var ch = this.props.html || this.props.children;
        return <span {...this.props} dangerouslySetInnerHTML={{__html: ch}} />;
    }
}

