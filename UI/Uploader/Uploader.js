import getRef from 'UI/Functions/GetRef';

var DEFAULT_ERROR = `Unable to upload`;
var DEFAULT_MESSAGE = `Drag and drop your file or click to upload here`;

const XHR_UNSENT = 0; // Client has been created.open() not called yet.
const XHR_OPENED = 1; // open() has been called.
const XHR_HEADERS_RECEIVED = 2;	// send() has been called, and headers and status are available.
const XHR_LOADING = 3; // Downloading; responseText holds partial data.
const XHR_DONE = 4; // The operation is complete.

/*
* General purpose file uploader. Doesn't delcare a form so can be used inline anywhere.
*/
export default class Uploader extends React.Component {

	constructor(props) {
		super(props);

		var message = this.props.label || DEFAULT_MESSAGE;

		if (props.iconOnly) {
			message = '';
        }

		this.inputRef = React.createRef();

		this.state = {
			loading: false,
			progressPercent: 0,
			progress: "",
			message: message,
			tooltip: message,
			maxSize: this.props.maxSize || 0,
			ref: this.props.currentRef,
			aspect169: this.props.aspect169,
			aspect43: this.props.aspect43,
			filename: this.props.currentRef ? getRef.parse(this.props.currentRef).ref : undefined,
			draggedOver: false
		};

		this.handleDragEnter = this.handleDragEnter.bind(this);
		this.handleDragLeave = this.handleDragLeave.bind(this);
	}

	onSelectedFile(e) {
		var file = e.target.files[0];

		this.setState({
			loading: true,
			failed: false,
			success: false,
			progressPercent: 0,
			progress: "",
			filename: file.name,
			ref: undefined
		});

		var maxSize = this.state.maxSize;

		if (maxSize > 0 && file.size > maxSize) {
			this.setState({
				loading: false,
				success: false,
				failed: `File too large`
			});

			return;
        }

		this.props.onStarted && this.props.onStarted(file, file);
		
		var xhr = new global.XMLHttpRequest();
		
		xhr.onreadystatechange = () => {

			if (xhr.readyState == XHR_DONE) {
				var uploadInfo;

				try {
					uploadInfo = JSON.parse(xhr.responseText);
				} catch(e) {
					
				}
				
				if (!uploadInfo || xhr.status > 300) {
					this.setState({
						loading: false,
						success: false,
						failed: uploadInfo && uploadInfo.message ? uploadInfo.message : DEFAULT_ERROR
					});

					return;
				}
				
				// uploadInfo contains the upload file info, such as its original public url and ref.
				
				// Run the main callback:
				this.props.onUploaded && this.props.onUploaded(uploadInfo);

				this.setState({
					loading: false,
					success: true,
					failed: false,
					ref: uploadInfo.result.ref
				});
				
				
			} else if (xhr.readyState == XHR_HEADERS_RECEIVED) {
				// Headers received
				if (xhr.status > 300){
					this.setState({
						loading: false,
						success: false,
						failed: DEFAULT_ERROR
					});
				}
			}
		};
		
		xhr.onerror = (e) => {
			console.log("XHR onerror", e);
			this.setState({
				loading: false,
				success: false,
				failed: DEFAULT_ERROR
			});
		};
		
		xhr.upload.onprogress = (evt) => {
			var pc = Math.floor(evt.loaded * 100 / evt.total);

			this.setState({
				progressPercent: pc,
				progress: ' ' + pc + '%'
			});
			this.props.onUploadProgress && this.props.onUploadProgress();
		};
		
		var ep = this.props.endpoint || "upload/create";
		
		var apiUrl = this.props.url || global.ingestUrl || global.apiHost || '';
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

	handleDragEnter() {
		this.setState({
			draggedOver: true
		});
    }

	handleDragLeave() {
		this.setState({
			draggedOver: false
		});
	}

	componentDidMount() {

		if (this.inputRef && this.inputRef.current) {
			var input = this.inputRef.current;
			input.addEventListener('dragenter', this.handleDragEnter);
			input.addEventListener('dragleave', this.handleDragLeave);
			input.addEventListener('drop', this.handleDragLeave);
        }

	}

	componentWillUnmount() {

		if (this.inputRef && this.inputRef.current) {
			var input = this.inputRef.current;
			input.removeEventListener('dragenter', this.handleDragEnter);
			input.removeEventListener('dragleave', this.handleDragLeave);
			input.removeEventListener('drop', this.handleDragLeave);
		}

	}

	componentWillReceiveProps(nextProps) {
		this.setState({
			ref: nextProps.currentRef,
			originalName: nextProps.originalName
		});
	}

	render() {
		const {
			loading,
			failed,
			progressPercent,
			progress,
			message,
			maxSize,
			ref,
			aspect169,
			aspect43,
			filename,
			draggedOver,
			tooltip,
			originalName
		} = this.state;

		var hasRef = ref && ref.length;
		var hasMaxSize = maxSize > 0;
		var hasFilename = (filename && filename.length);
		var hasOriginalName = (originalName && originalName.length);
		var label = loading ? (`Uploading` + " " + progress + " ...") : message;
		var canShowImage = getRef.isImage(ref);
		var canShowVideo = getRef.isVideo(ref,false);
		var canShowIcon = getRef.isIcon(ref);
		var labelStyle = {};
		var uploaderClasses = ['uploader'];
		var uploaderLabelClasses = ['uploader__label'];

		if (this.props.compact) {
			uploaderClasses.push("uploader--compact");
		}

		if (loading) {
			uploaderClasses.push("uploader--progress");
        }

		if (failed) {
			uploaderClasses.push("uploader--error");
			label = failed;
		}

		if (aspect169) {
			uploaderClasses.push("uploader--16-9");
		}

		if (aspect43) {
			uploaderClasses.push("uploader--4-3");
		}

		if (draggedOver) {
			uploaderClasses.push("uploader--drag-target");
		}

		var iconClass = "";
		var iconName = "";

		if (hasRef) {
			var refInfo = getRef.parse(ref);

			uploaderClasses.push("uploader--content");
			label = "";

			// TODO: check original image width/height values here; if both are less than 256px,
			// use the original image and set background-size to auto
			if (canShowImage && !canShowVideo && !canShowIcon) {
				labelStyle = { "background-image": "url(" + getRef(ref, { url: true, size: 256 }) + ")" };
			}

			if ((canShowImage || canShowVideo)&& !canShowIcon) {
				uploaderClasses.push("uploader--image");
			}

			if(canShowVideo) {
				uploaderLabelClasses.push("video");
			}

			if (canShowIcon) {
				iconClass = refInfo.scheme + " " + refInfo.ref + " uploader__file";
				iconName = refInfo.ref;
            }

        }

		var uploaderClass = uploaderClasses.join(' ');
		var uploaderLabelClass = uploaderLabelClasses.join(' ');

		var renderedSize = 256;

		var caption = hasFilename ? filename : `None selected`;

		if (hasOriginalName) {
			caption = originalName;
        }

		if (canShowIcon) {
			caption = iconName;
        }

		return <div className={uploaderClass}>
			<div className={this.props.iconOnly ? "uploader__internal uploader__internal--icon" : "uploader__internal"}>

				{(canShowImage || canShowVideo) && !canShowIcon && 
					<div className="uploader__imagebackground">
					</div>
				}

				<input id={this.props.id} className="uploader__input" type="file" disabled={this.props.iconOnly} ref={this.inputRef}
					onChange={e => this.onSelectedFile(e)} title={loading ? "Loading ..." : tooltip} />
				<label htmlFor={this.props.id} className={uploaderLabelClass} style={labelStyle}>

					{/* loading */}
					{loading && <>
						<div class="spinner-border" role="status"></div>
					</>}

					{/* has a reference, but isn't an image */}
					{hasRef && !canShowImage && !canShowVideo && !canShowIcon && <>
						<i className="fal fa-file uploader__file" />
					</>}

					{/* has an video  reference */}
					{hasRef && canShowVideo && getRef(ref, { size: renderedSize })}

					{/* has an icon reference */}
					{hasRef && canShowIcon && <>
						<i className={iconClass} />
					</>}

					{/* failed to upload */}
					{failed && <>
						<i class="fas fa-times-circle"></i>
					</>}

					<span className="uploader__label-internal">
						{label}
					</span>
				</label>
				{loading && (
					<progress className="uploader__progress" max="100" value={progressPercent}></progress>
				)}
			</div>
			<small className="uploader__caption text-muted">
				{caption}
				<br />

				{hasMaxSize && <>
					Max file size: {this.formatBytes(maxSize)}
				</>}
			</small>
        </div>;
    }
}