import getRef from 'UI/Functions/GetRef';

var DEFAULT_ERROR = `Unable to upload`;
var DEFAULT_MESSAGE = `Drag and drop your file or click to upload here`;
var DEFAULT_MESSAGE_MULTIPLE = `Drag and drop your file(s) or click to upload here`;

const XHR_UNSENT = 0; // Client has been created.open() not called yet.
const XHR_OPENED = 1; // open() has been called.
const XHR_HEADERS_RECEIVED = 2;	// send() has been called, and headers and status are available.
const XHR_LOADING = 3; // Downloading; responseText holds partial data.
const XHR_DONE = 4; // The operation is complete.

/*
* General purpose file uploader. Doesn't declare a form so can be used inline anywhere.
*/
export default class Uploader extends React.Component {

	constructor(props) {
		super(props);

		var defaultMessage = this.props.multiple ? DEFAULT_MESSAGE_MULTIPLE : DEFAULT_MESSAGE;
		var message = this.props.label || defaultMessage;

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
			files: [],
			draggedOver: false
		};

		this.handleDragEnter = this.handleDragEnter.bind(this);
		this.handleDragLeave = this.handleDragLeave.bind(this);
	}

	onSelectedFile(e) {
		var maxSize = this.state.maxSize;
		var newFiles = [];

		[...e.target.files].forEach((file, i) => {
			var fileInfo = {
				loading: true,
				failed: false,
				success: false,
				progressPercent: 0,
				progress: "",
				filename: file.name,
				ref: undefined
			};
			newFiles.push(fileInfo);
		});

		this.setState({
			files: newFiles
		}, () => {
			[...e.target.files].forEach((file, i) => {

				this.setState({
					loading: true,
					failed: false,
					success: false,
					progressPercent: 0,
					progress: "",
					filename: file.name,
					ref: undefined
				});

				if (maxSize > 0 && file.size > maxSize) {
					var files = [...this.state.files];
					var info = files[i];
					info.loading = false;
					info.success = false;
					info.failed = `File too large`;

					this.setState({
						loading: false,
						success: false,
						failed: `File too large`,
						files: files
					});

					return;
				}

				this.props.onStarted && this.props.onStarted(file, file);

				var xhr = new global.XMLHttpRequest();
				//xhr.upload.fileIndex = i;

				xhr.onreadystatechange = () => {

					if (xhr.readyState == XHR_DONE) {
						var uploadInfo;

						try {
							uploadInfo = JSON.parse(xhr.responseText);
						} catch (e) {

						}

						if (!uploadInfo || xhr.status > 300) {
							var msg = uploadInfo && uploadInfo.message ? uploadInfo.message : DEFAULT_ERROR;
							var files = [...this.state.files];
							var info = files[i];
							info.loading = false;
							info.success = false;
							info.failed = msg;

							this.setState({
								loading: false,
								success: false,
								failed: msg,
								files: files
							});

							return;
						}

						// uploadInfo contains the upload file info, such as its original public url and ref.

						// Run the main callback:
						this.props.onUploaded && this.props.onUploaded(uploadInfo);

						var files = [...this.state.files];
						var info = files[i];
						info.loading = false;
						info.success = true;
						info.failed = false;
						info.ref = uploadInfo.result.ref;

						this.setState({
							loading: false,
							success: true,
							failed: false,
							ref: uploadInfo.result.ref,
							files: files
						});


					} else if (xhr.readyState == XHR_HEADERS_RECEIVED) {
						// Headers received
						if (xhr.status > 300) {
							var files = [...this.state.files];
							var info = files[i];
							info.loading = false;
							info.success = false;
							info.failed = DEFAULT_ERROR;

							this.setState({
								loading: false,
								success: false,
								failed: DEFAULT_ERROR,
								files: files
							});
						}
					}
				};

				xhr.onerror = (e) => {
					console.log("XHR onerror", e);
					var files = [...this.state.files];
					var info = files[i];
					info.loading = false;
					info.success = false;
					info.failed = DEFAULT_ERROR;

					this.setState({
						loading: false,
						success: false,
						failed: DEFAULT_ERROR,
						files: files
					});
				};

				xhr.upload.onprogress = (evt) => {
					var pc = Math.floor(evt.loaded * 100 / evt.total);
					var files = [...this.state.files];
					var info = files[i];
					info.progressPercent = pc;
					info.progress = ' ' + pc + '%'

					this.setState({
						progressPercent: pc,
						progress: ' ' + pc + '%',
						files: files
					});

					this.props.onUploadProgress && this.props.onUploadProgress();
				};

				var ep = this.props.endpoint || "upload/create";

				var apiUrl = this.props.url || global.ingestUrl || global.apiHost || '';
				if (!apiUrl.endsWith('/')) {
					apiUrl += '/';
				}
				apiUrl += 'v1/';

				ep = (ep.indexOf('http') === 0 || ep[0] == '/') ? ep : apiUrl + ep;

				xhr.open('PUT', ep, true);

				var { requestOpts } = this.props;

				if (requestOpts && requestOpts.headers) {
					for (var header in requestOpts.headers) {
						xhr.setRequestHeader(header, requestOpts.headers[header]);
					}
				}

				xhr.setRequestHeader("Content-Name", file.name);
				xhr.setRequestHeader("Private-Upload", this.props.isPrivate ? '1' : '0');
				xhr.send(file);
			});
		});

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

	renderBulkUploadUI() {
		var uploaderClasses = ['uploader', 'uploader--multiple'];

		if (this.props.compact) {
			uploaderClasses.push("uploader--compact");
		}

		if (this.state.aspect169) {
			uploaderClasses.push("uploader--16-9");
		}

		if (this.state.aspect43) {
			uploaderClasses.push("uploader--4-3");
		}

		if (this.state.draggedOver) {
			uploaderClasses.push("uploader--drag-target");
		}

		return <div className={uploaderClasses.join(' ')}>
			{/* prompt to upload */}
			{!this.state.files || !this.state.files.length && <>
				<div className="uploader__internal">
					<input id={this.props.id} className="uploader__input" type="file" disabled={this.props.iconOnly} ref={this.inputRef}
						onChange={e => this.onSelectedFile(e)} title={this.state.tooltip} multiple />
					<label htmlFor={this.props.id} className="uploader__label">
						<span className="uploader__label-internal">
							{this.state.message}
						</span>
					</label>
				</div>
			</>}

			{/* display selected files */}
			{this.state.files && this.state.files.length > 0 && <>
				<div className="uploader__bulk-list">
					{this.state.files.map(file => {
						var fileClasses = ['upload'];
						var labelClasses = ['uploader__label'];
						var fileLabel;

						if (file.loading) {
							fileClasses.push("uploader--progress");
							fileLabel = `Uploading ${file.progress} ...`;
                        }
						
						if (file.failed) {
							fileClasses.push("uploader--error");
							fileLabel = failed;
						}

						var hasRef = file.ref && file.ref.length;
						var hasFilename = file.filename && file.filename.length;
						var hasOriginalName = file.originalName && file.originalName.length;
						var iconClass = "";
						var iconName = "";
						var labelStyle = {};

						if (hasRef) {
							var refInfo = getRef.parse(file.ref);
							var canShowImage = getRef.isImage(file.ref);
							var canShowVideo = getRef.isVideo(file.ref, false);
							var canShowIcon = getRef.isIcon(file.ref);

							fileClasses.push("uploader--content");
							fileLabel = "";

							// TODO: check original image width/height values here; if both are less than 256px,
							// use the original image and set background-size to auto
							if (canShowImage && !canShowVideo && !canShowIcon) {
								labelStyle = { "background-image": "url(" + getRef(file.ref, { url: true, size: 256 }) + ")" };
							}

							if ((canShowImage || canShowVideo) && !canShowIcon) {
								fileClasses.push("uploader--image");
							}

							if (canShowVideo) {
								labelClasses.push("video");
							}

							if (canShowIcon) {
								iconClass = refInfo.scheme + " " + refInfo.ref + " uploader__file";
								iconName = refInfo.ref;
							}

						}

						var renderedSize = 256;
						var caption = hasFilename ? file.filename : false;

						if (hasOriginalName) {
							caption = originalName;
						}

						if (canShowIcon) {
							caption = iconName;
						}

						return <div className={fileClasses.join(' ')}>
							<div className="uploader__internal">

								{(canShowImage || canShowVideo) && !canShowIcon &&
									<div className="uploader__imagebackground">
									</div>
								}

								<label className={labelClasses.join(' ')} style={labelStyle}>

									{/* loading */}
									{file.loading && <>
										<div class="spinner-border" role="status"></div>
									</>}

									{/* has a reference, but isn't an image */}
									{hasRef && !canShowImage && !canShowVideo && !canShowIcon && <>
										<i className="fal fa-file uploader__file" />
									</>}

									{/* has an video reference */}
									{hasRef && canShowVideo && getRef(file.ref, { size: renderedSize })}

									{/* has an icon reference */}
									{hasRef && canShowIcon && <>
										<i className={iconClass} />
									</>}

									{/* failed to upload */}
									{file.failed && <>
										<i class="fas fa-times-circle"></i>
									</>}

									<span className="uploader__label-internal">
										{fileLabel}
									</span>
								</label>
								{file.loading && (
									<progress className="uploader__progress" max="100" value={file.progressPercent}></progress>
								)}
							</div>
							{caption && <>
								<small className="uploader__caption text-muted">
									{caption}
								</small>
							</>}
						</div>;
					})}
				</div>
			</>}

		</div>;
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

		var isMultiple = this.props.multiple;

		if (isMultiple) {
			return this.renderBulkUploadUI();
        }

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
					onChange={e => this.onSelectedFile(e)} title={loading ? `Loading ...` : tooltip} />
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
			{!isMultiple && <>
				<small className="uploader__caption text-muted">
					{caption}
					<br />

					{hasMaxSize && <>
						{`Max file size: ${this.formatBytes(maxSize)}`}
					</>}
				</small>
			</>}
        </div>;
    }
}