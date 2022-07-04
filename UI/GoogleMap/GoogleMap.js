// Module import examples - none are required:
// import webRequest from 'UI/Functions/WebRequest';
// import Loop from 'UI/Loop';

var loaded = null;

function loadMaps(key){
	if(loaded){
		return loaded;
	}
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
			var c = this.props.center;
			var center = {
				lat: c ? parseFloat(c.lat) : 51.511536,
				lng: c ? parseFloat(c.lng) : -0.122147
			};
			var map = new google.maps.Map(this.mapEle, {
				center,
				zoom: this.props.zoom || 8
			});
			
			var markerSet = this.props.markers;
			
			if(markerSet){
				for(var i=0;i<markerSet.length;i++){
					((info) => {
						var marker = new google.maps.Marker({
							position: {
								lat: parseFloat(info.lat),
								lng: parseFloat(info.lng)
							},
							map: map,
							title: info.title || ''
						});
						
						if(!this.props.notClickable){
							marker.addListener('click', () => {
								this.props.onMarkerClick && this.props.onMarkerClick(info);
							});
						}
					})(markerSet[i]);
				}
			}
			
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
