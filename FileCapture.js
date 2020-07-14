import Alert from 'UI/Alert';

export default class FileCapture extends React.Component {
    constructor(props) {
        super(props);
        this.selectImage = this.selectImage.bind(this)
    }
    
    selectImage(event) {
		var file = event.target.files[0];
		this.props.onChange && this.props.onChange(file, file.name);
        this.setState({file: URL.createObjectURL(file)});
    }
    
    render() {
		var accept = 'image/*';
		var capture = 'environment';
		var icon = 'fas fa-image';
		var ctaText = 'Upload Image';
		var nameText = 'Image';
		
		if(this.props.audio){
			capture = 'microphone';
			ctaText = 'Record Audio';
			nameText = 'Audio';
			icon = 'fas fa-microphone';
			accept = 'audio/*';
		}else if(this.props.video){
			ctaText = 'Record Video';
			nameText = 'Video';
			icon = 'fas fa-video';
			accept = 'video/*';
		}
		
		if(this.props.live === false){
			capture = undefined;
		}
		
        return (
            this.state.file ? (
                <div>
                    <div style = {{width: "100%"}} className = "btn btn-primary" onClick = {() => {
						this.setState({file: null});
						this.props.onChange && this.props.onChange(null);
					}}><i className="fas fa-trash-alt" />{'Delete ' + nameText}</div>
                    <div>
						{
							this.props.video && (
								<video controls style={{width: "100%"}} src={this.state.file}  />
							)
						}
						{
							this.props.audio && (
								<audio controls style={{width: "100%"}} src={this.state.file} />
							)
						}
						{
							!this.props.audio && !this.props.video && (
								<img src = {this.state.file} style={{width: "100%", height: "auto"}} />
							)
						}
                    </div>
                </div>
            ) : (
                <div className="file-capture" style = {{position: "relative", width: "100%"}}> 
                    <div style = {{width: "100%"}} className = "btn btn-primary"><i className={icon}></i> {ctaText}</div>
                    <input className = "hiddenInput" type='file' onChange = {this.selectImage} accept={accept} capture={capture} />
                </div>
            )
        );
    }
}