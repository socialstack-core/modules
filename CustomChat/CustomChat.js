export default class CustomChat extends React.Component {
	
	constructor(props){
		super(props);
	}
	
	render(){
		// Non-react owned DOM element. Do not add any other props to this div - it must only have ref on it.
		return <div ref={targetZone => {
			if(!this.props.root){
				return;
			}
			if(targetZone){
				while(targetZone.firstChild) targetZone.removeChild(targetZone.lastChild);
				
				this.props.root.style.display='';
				targetZone.appendChild(this.props.root);
			}else{
				this.props.root.parentNode.removeChild(this.props.root);
			}
		}} />
	}
	
}