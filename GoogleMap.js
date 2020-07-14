// Module import examples - none are required:
// import webRequest from 'UI/Functions/WebRequest';
// import Loop from 'UI/Loop';

var loaded = null;

function loadMaps(key){
	if(loaded){
		return loaded;
	}
	console.log(key);
	return loaded = new Promise((s, r) => {
		var script = global.document.createElement('script');
		script.src = 'https://maps.googleapis.com/maps/api/js?key=' + key + '&callback=stackInitGMap';
		script.defer = true;
		script.async = true;
		
		global.stackInitGMap = function() {
			s();
		};
		
		global.document.head.appendChild(script);
	});
}

export default class GoogleMap extends React.Component {
	
	constructor(props){
		super(props);
		this.state={};
	}
	
	componentWillReceiveProps(props){
		var {map} = this.state;
		if(!map){
			return;
		}
		
	}
	
	componentDidMount(){
		loadMaps(this.props.apiKey).then(() => {
			var map = new google.maps.Map(this.mapEle, {
				center: this.props.center || {
					lat: 51.511536,
					lng: -0.122147
				},
				zoom: this.props.zoom || 8
			});
			
			console.log(map);
			
			this.setState({
				map
			});
			
		});
	}
	
	render(){
		
		return <div className="google-map" ref={e => this.mapEle = e} />;
		
	}
	
}

GoogleMap.propTypes = {
	
};
