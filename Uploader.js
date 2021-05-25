import Loading from 'UI/Loading';
import Alert from 'UI/Alert';

/*
* General purpose file uploader. Doesn't delcare a form so can be used inline anywhere.
*/
export default class Uploader extends React.Component {

	constructor(props) {
		super(props);
		this.state = {
			loading: false,
			progress: ""
		};
	}
	
	onSelectedFile(e) {
		this.setState({ loading: true, failed: false, success: false,progress: "" });
		var formData = new FormData();
		var file = e.target.files[0];
		formData.append('file', file);
		this.props.onStarted && this.props.onStarted(file, formData);
		
		var xhr = new global.XMLHttpRequest();
			
		xhr.onreadystatechange = () => {
			if (xhr.readyState == 4) {
				if (xhr.responseText) {
					var uploadInfo;
					try{
						uploadInfo = JSON.parse(xhr.responseText);
					}catch(e){
						
					}
					console.log(uploadInfo);
					
					if(!uploadInfo || uploadInfo.errors){
						this.setState({loading: false, success: false, failed: true});
						return;
					}
					
					// uploadInfo contains the upload file info, such as its original public url and ref.
					
					// Run the main callback:
					this.props.onUploaded && this.props.onUploaded(uploadInfo);
					
					this.setState({loading: false, success: true, failed: false});
				}
			}
		};
		
		xhr.upload.onprogress = (evt) => {
			this.setState({
				progress: ' ' + Math.floor(evt.loaded * 100 / evt.total) + '%'
			});
			this.props.onUploadProgress && this.props.onUploadProgress();
		};
		
		xhr.open('POST', global.ingestUrl ? global.ingestUrl + "v1/upload/create" : ((global.apiHost || '') + '/v1/upload/create'), true);
		xhr.send(formData);
	}

	formatBytes(bytes, decimals = 2) {

		if (bytes === 0) {
			return "";
		}

		const k = 1024;
		const dm = decimals < 0 ? 0 : decimals;
		const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];

		const i = Math.floor(Math.log(bytes) / Math.log(k));

		return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
	}
	
    render() {
		const {
			loading,
			failed,
			progress
		} = this.state;

        return (
			<div className="uploader">
				{loading && (
					<Loading message={"Uploading" + progress + ".."}/>
				)}
				{failed && (
					<Alert type="error">
						Unable to upload that file - it may be a format we don't support.
					</Alert>
				)}
				{!loading && (
					<input type="file" id={this.props.id} onChange={e => this.onSelectedFile(e)} className="form-control-file" />
				)}
				{this.props.id && !loading &&
					<label htmlFor={this.props.id}>
						{this.props.label || "Upload file"}
					</label>
				}
				{this.props.maxSize && 
					<span className="upload-limit">
						Max file size: {this.formatBytes(this.props.maxSize)}
					</span>
				}
			</div>
        );
    }
}