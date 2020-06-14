import getRef from 'UI/Functions/GetRef';

export default class Backdrop extends React.Component {
	
	render(){
		
		return <div className="backdrop" style={{backgroundImage: 'url(' + getRef(this.props.imageRef, {url: true}) + ')'}}>
			{this.props.children}
		</div>;
		
	}
	
}

Backdrop.propTypes = {
	imageRef: 'image',
	children: true
};