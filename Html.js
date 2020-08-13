/**
 * This component displays html. Only ever use this with trusted text.
*/

export default class Html extends React.Component {

    constructor(props) {
        super(props);
    }
	
    render() {
		return <span className={this.props.className} dangerouslySetInnerHTML={{__html: this.props.children}} />;
    }
}

