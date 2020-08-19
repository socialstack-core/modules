import Loading from 'UI/Loading';
import Alert from 'UI/Alert';
import webRequest from 'UI/Functions/WebRequest';

/*
* General purpose file uploader. Doesn't delcare a form so can be used inline anywhere.
*/
export default class Uploader extends React.Component {

	constructor(props) {
		super(props);
		this.state = {
			loading: false
		};
	}
	
	onSelectedFile(e) {
		this.setState({ loading: true, failed: false, success: false });
		var formData = new FormData();
		var file = e.target.files[0];
		formData.append('file', file);
		this.props.onStarted && this.props.onStarted(file, formData);
		
		webRequest("uploader/upload", formData, {
			onUploadProgress: this.props.onUploadProgress
		}).then(response => {
			
			var uploadInfo = response.json;
			
			if(!uploadInfo){
				this.setState({loading: false, success: false, failed: true});
				return;
			}
			
			// uploadInfo contains the upload file info, such as its original public url and ref.
			
			// Run the main callback:
			this.props.onUploaded && this.props.onUploaded(uploadInfo);
			
			this.setState({loading: false, success: true, failed: false});
		});
		
	}
	
    render() {
		const {
			loading,
			failed
		} = this.state;
		
        return (
			<div className="uploader">
				{loading && (
					<Loading message="Uploading.."/>
				)}
				{failed && (
					<Alert type="error">
						Unable to upload that file - it may be a format we don't support.
					</Alert>
				)}
				<input type="file" onChange={e => this.onSelectedFile(e)} className="form-control-file" />
			</div>
        );
    }
}