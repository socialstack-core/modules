/*
Just an invisible space of a specified height. The default is 20px.
*/


export default (props) => <div className="spacer-container">
		<div className="spacer" style={{height: props.height ? (props.height + 'px') : '20px'}}/>
	</div>