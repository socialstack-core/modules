/*
Just an invisible space of a specified height. The default is 20px.
*/

export default function Spacer (props) {
	return <div className="spacer-container">
		<div className="spacer" style={{height: props.height ? (props.height + 'px') : '20px'}}/>
	</div>;
}

Spacer.propTypes = {
	height: 'int'
};

Spacer.icon = 'sort';