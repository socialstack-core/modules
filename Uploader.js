import Loading from 'UI/Loading';
import Alert from 'UI/Alert';

var DEFAULT_ERROR = `Unable to upload that file - this often means it was too big.`;

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
		var file = e.target.files[0];
		this.props.onStarted && this.props.onStarted(file, file);
		
		var xhr = new global.XMLHttpRequest();
		
		xhr.onreadystatechange = () => {
			if (xhr.readyState == 4) {
				var uploadInfo;
				try{
					uploadInfo = JSON.parse(xhr.responseText);
				}catch(e){
					
				}
				
				if(!uploadInfo || xhr.status > 300){
					this.setState({loading: false, success: false, failed: uploadInfo && uploadInfo.message ? uploadInfo.message : DEFAULT_ERROR});
					return;
				}
				
				// uploadInfo contains the upload file info, such as its original public url and ref.
				
				// Run the main callback:
				this.props.onUploaded && this.props.onUploaded(uploadInfo);
				
				this.setState({loading: false, success: true, failed: false});
				
				
			}else if(xhr.readyState == 2) {
				// Headers received
				
				if(xhr.status > 300){
					this.setState({loading: false, success: false, failed: DEFAULT_ERROR});
				}
			}
		};
		
		xhr.onerror = (e) => {
			console.log("XHR onerror", e);
			this.setState({loading: false, success: false, failed: DEFAULT_ERROR});
		};
		
		xhr.upload.onprogress = (evt) => {
			this.setState({
				progress: ' ' + Math.floor(evt.loaded * 100 / evt.total) + '%'
			});
			this.props.onUploadProgress && this.props.onUploadProgress();
		};
		
		var ep = this.props.endpoint || "upload/create";
		
		var apiUrl = global.ingestUrl || global.apiHost || '';
		if(!apiUrl.endsWith('/')){
			apiUrl += '/';
		}
		apiUrl += 'v1/';
		
		ep = (ep.indexOf('http') === 0 || ep[0] == '/') ? ep : apiUrl + ep;
		
		xhr.open('PUT', ep, true);
		
		var {requestOpts} = this.props;
		
		if(requestOpts && requestOpts.headers){
			for(var header in requestOpts.headers){
				xhr.setRequestHeader(header, requestOpts.headers[header]);
			}
		}
		
		xhr.setRequestHeader("Content-Name", file.name);
		xhr.setRequestHeader("Private-Upload", this.props.isPrivate ? '1' : '0');
		xhr.send(file);
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
					{failed}
					</Alert>
				)}
				{!loading && (
					<input type="file" id={this.props.id} onChange={e => this.onSelectedFile(e)} className="form-control-file" />
				)}
				{this.props.id && !loading &&
					<label htmlFor={this.props.id} className="btn btn-outline-primary">
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