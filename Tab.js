/*
	tab panel.
*/

export default class Tab extends React.Component {

    state = {
        activeIndex: this.props.defaultIndex || 0
    };

	render(){
        return (
            this.props.children
        );
    }
}
