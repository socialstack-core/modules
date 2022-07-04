import isNumeric from 'UI/Functions/IsNumeric';

export default class Youtube extends React.Component {
	
	render(){
		var { videoId, width, height } = this.props;
		
		if(!height){
			height = width && isNumeric(width) ? width * 0.5625 : 315; // 16:9 is the Youtube default
		}
		
		if(!width){
			width = 560;
		}
		
		return <iframe width={width} height={height} src={"https://www.youtube.com/embed/" + videoId} frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen />
		
	}
	
}

Youtube.propTypes={
	videoId: 'string',
	width: 'int',
	height: 'int'
};