/*
	tab panel.
*/

export default class Tab extends React.Component {

	constructor(props){
		super(props);
		this.state = {
			activeIndex: this.props.defaultIndex || 0
		};
	}

	render(){
        return (
            this.props.children
        );
    }
}
