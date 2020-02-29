import getRef from 'UI/Functions/GetRef';

/*
Used to display a video from a fileRef. Can be e.g. youtube:VideoID
Min required props: fileRef.
<Video fileRef='public:2.mp4'/>
*/
export default class Video extends React.Component {
	render(){
		var ref = this.props.fileRef;
		var width = this.props.width || 560;
		var height = this.props.height || 315;
		
		if(!ref){
			return (<div style={{width,height,backgroundColor: 'grey', color: 'white', textAlign: 'center', display: 'inline-block'}}>
				<div style={{margin: '10px'}}>
					<i className='fa fa-play' />
				</div>
				No source
			</div>);
		}
		
		// Ideally these become submodules where the ref is handled generically.
		if(ref.indexOf('youtube:') === 0){
			
			var videoId = ref.substring(8);
			
			return (
				<iframe
					width={width}
					height={height}
					src={"https://www.youtube.com/embed/" + videoId}
					frameborder="0"
					allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture"
					allowfullscreen
				/>);
			
		}else if(ref.indexOf('vimeo:') === 0){
			
			return (
				<iframe
					src="https://player.vimeo.com/video/265045525"
					width={width}
					height={height}
					frameborder="0"
					allow="autoplay; fullscreen"
					allowfullscreen
				/>);
			
		}
		
		/* assuming mp4 for now */
		return (
			<video
				width={width}
				height={height}
				controls
			>
				<source
					src={getRef(this.props.fileRef, {url: true})}
					type="video/mp4"
				/>
				Your browser does not support this video.
			</video>
		);
	}
}

Video.propTypes = {
	fileRef: 'string',
	width: 'int',
	height: 'int'
};

Video.icon = 'play-circle';