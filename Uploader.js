import Loading from 'UI/Loading';
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
		this.setState({ loading: true });
		var formData = new FormData();
		var file = e.target.files[0];
		formData.append('file', file);
		this.props.onStarted && this.props.onStarted(file, formData);
		
		webRequest("uploader/upload", formData, {
			onUploadProgress: this.props.onUploadProgress
		}).then(response => {
			
			var uploadInfo = response.json;
			// uploadInfo contains the upload file info, such as its original public url and ref.
			
			// Run the main callback:
			this.props.onUploaded && this.props.onUploaded(uploadInfo);
			
			this.setState({loading: false, success: true});
		});
		
	}
	
    render() {
		const {
			loading
		} = this.state;
		
        return (
			<div className="uploader">
				{loading && (
					<Loading />
				)}
				<input type="file" id={this.props.id} onChange={e => this.onSelectedFile(e)} className="form-control-file" />
				{this.props.id &&
					<label htmlFor={this.props.id}></label>
				}
			</div>
        );
    }
}