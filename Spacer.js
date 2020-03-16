/*
Just an invisible space of a specified height. The default is 20px.
*/

export default class Spacer extends React.Component {
	
	render(){
		return <div className="spacer-container">
			<div className="spacer" style={{height: this.props.height ? (this.props.height + 'px') : '20px'}}/>
		</div>
	}
}

Spacer.propTypes = {
	height: 'int'
};

Spacer.icon = 'sort';