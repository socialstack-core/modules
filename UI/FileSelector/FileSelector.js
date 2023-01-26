import Loop from 'UI/Loop';
import Modal from 'UI/Modal';
import Uploader from 'UI/Uploader';
import omit from 'UI/Functions/Omit';
import getRef from 'UI/Functions/GetRef';
import IconSelector from 'UI/FileSelector/IconSelector';
import Dropdown from 'UI/Dropdown';
import Col from 'UI/Column';
import Input from 'UI/Input';
import Debounce from 'UI/Functions/Debounce';

var inputTypes = global.inputTypes = global.inputTypes || {};
let lastId = 0;

inputTypes.ontypefile = inputTypes.ontypeimage = function (props, _this) {
	return (
		<FileSelector
			id={props.id || _this.fieldId}
			className={props.className || "form-control"}
			{...omit(props, ['id', 'className', 'type', 'inline'])}
		/>
	);
};

inputTypes.ontypeicon = function (props, _this) {
	return (
		<FileSelector
			id={props.id || _this.fieldId}
			iconOnly
			className={props.className || "form-control"}
			{...omit(props, ['id', 'className', 'type', 'inline'])}
		/>
	);
};

inputTypes.ontypeupload = function (props, _this) {
	return (
		<FileSelector
			id={props.id || _this.fieldId}
			browseOnly
			className={props.className || "form-control"}
			{...omit(props, ['id', 'className', 'type', 'inline'])}
		/>
	);
};

inputTypes.ontypenopaging = function (props, _this) {
	return (
		<FileSelector
			id={props.id || _this.fieldId}
			disablePaging
			className={props.className || "form-control"}
			{...omit(props, ['id', 'className', 'type', 'inline'])}
		/>
	);
};

/**
 * Select a file from a users available uploads, outputting a ref.
 * You can use <Input type="file" .. /> to obtain one of these.
 */
export default class FileSelector extends React.Component {

	constructor(props) {
		super(props);

		this.search = this.search.bind(this);

		this.state = {
			ref: props.value || props.defaultValue,
			debounce: new Debounce(this.search)
		};

		this.closeModal = this.closeModal.bind(this);
	}


	newId() {
		lastId++;
		return `fileselector${lastId}`;
	}

	updateValue(e, newRef) {
		if(e) {
			e.preventDefault ();
		}

		var originalName = newRef ? newRef.originalName : '';

		if (!newRef) {
			newRef = '';
		}

		if (newRef.result && newRef.result.ref) {
			// Accept upload objects also.
			newRef = newRef.result.ref;
		} else if (newRef.ref) {
			newRef = newRef.ref;
		}

		this.props.onChange && this.props.onChange({ target: { value: newRef } });

		this.setState({
			value: newRef,
			originalName: originalName,
			modalOpen: false
		});
	}

	showModal() {
		this.setState({ modalOpen: true });
	}

	closeModal() {
		this.setState({ modalOpen: false });
	}

	search(query) {
		console.log(query);
		this.setState({ searchFilter: query.toLowerCase() })
	}

	renderHeader() {
		return <div className="row header-container">
			<Col size={4}>
				<label htmlFor="file-search">
					{`Search`}
				</label>
				<Input type="text" value={this.state.searchFilter} name="file-search" onKeyUp={(e) => {
					this.state.debounce.handle(e.target.value);
				}} />
			</Col>
		</div>;
	}

	render() {

		var { searchFilter } = this.state;

		var currentRef = this.props.value || this.props.defaultValue;

		if (this.state.value !== undefined) {
			currentRef = this.state.value;
		}

		var hasRef = currentRef && currentRef.length;
		var filename = hasRef ? getRef.parse(currentRef).ref : "";
		var originalName = this.state.originalName && this.state.originalName.length ? this.state.originalName : '';

		if (originalName) {
			filename = originalName;
		}
		
		var source = "upload/list";
		if (this.props.showActive ) {
		 	source = "upload/active";
		}

		return <div className="file-selector">

			{/* upload browser */}
			<Modal
				isExtraLarge
				title={`Select an Upload`}
				className={"image-select-modal"}
				buttons={[
					{
						label: `Close`,
						onClick: this.closeModal
					}
				]}
				onClose={this.closeModal}
				visible={this.state.modalOpen}
			>
				{this.renderHeader()}
				<div className="file-selector__grid">
					<Loop raw over={source} filter={{ sort: { field: 'CreatedUtc', direction: 'desc' } }} paged={this.props.disablePaging ? undefined : true}>
						{
							entry => {

								if (entry.originalName.toLowerCase().includes(searchFilter) || entry.originalName.toLowerCase().replace(/-/g, " ").includes(searchFilter) || !searchFilter) {

									// NB: API has been seen to report valid images with isImage=false
									//var isImage = entry.isImage;
									var isImage = getRef.isImage(entry.ref);

									// default to 256px preview
									var renderedSize = 256;
									var imageWidth = parseInt(entry.width, 10);
									var imageHeight = parseInt(entry.height, 10);
									var previewClass = "file-selector__preview ";

									// render image < 256px if original image size was smaller
									if (!isNaN(imageWidth) && !isNaN(imageHeight) &&
										imageWidth < renderedSize && imageHeight < renderedSize) {
										renderedSize = undefined;
										previewClass += "file-selector__preview--auto";
									}

									return <>
										<div class="loop-item">
											<button title={entry.originalName} type="button" className="btn file-selector__item" onClick={(e) => this.updateValue(e, entry)}>
												<div className={previewClass}>
													{isImage && getRef(entry.ref, { size: renderedSize })}
													{!isImage && (
														<i className="fal fa-4x fa-file"></i>
													)}
												</div>
												<span className="file-selector__name">
													{entry.originalName}
												</span>
											</button>
										</div>
									</>;
								}
							}
						}
					</Loop>
				</div>
			</Modal>

			{/* icon browser */}
			<IconSelector
				visible={this.state.iconModalOpen}
				onClose={() => {
					this.setState({ iconModalOpen: false })
				}}
				onSelected={
					icon => {
						console.log("onSelected");
						console.log(icon);
						this.updateValue(null,icon);
					}
				}
			/>

			{/* upload */}
			<Uploader
				compact={this.props.compact}
				currentRef={currentRef}
				originalName={originalName}
				id={this.props.id || this.newId()}
				isPrivate={this.props.isPrivate}
				url={this.props.url}
				requestOpts={this.props.requestOpts}
				maxSize={this.props.maxSize}
				iconOnly={this.props.iconOnly}
				onUploaded={
					file => this.updateValue(null,file)
				} />

			{/* options (browse, preview, remove) */}
			<div className="file-selector__options">
				{!this.props.browseOnly && <>

					{this.props.uploadOnly && 
						<button type="button" className="btn btn-primary file-selector__select" onClick={() => this.showModal()}>
							{`Select upload`}
						</button>
					}

					{this.props.iconOnly &&
						<button type="button" className="btn btn-primary file-selector__select" onClick={() => this.setState({ iconModalOpen: true })}>
							{`Select icon`}
						</button>
					}
					
					{!this.props.uploadOnly && !this.props.iconOnly && 
							<Dropdown label={`Select`} variant="primary" className="file-selector__select">
							<li>
								<button type="button" className="btn dropdown-item" onClick={() => this.showModal()}>
									{`From uploads`}
								</button>
							</li>
							<li>
								<button type="button" className="btn dropdown-item" onClick={() => this.setState({ iconModalOpen: true })}>
									{`From icons`}
								</button>
							</li>
						</Dropdown>
					}

				</>}
				{hasRef && <>

					{!getRef.isIcon(currentRef) && <>
						<a href={getRef(currentRef, { url: true })} alt={filename} className="btn btn-primary file-selector__link" target="_blank" rel="noopener noreferrer">
							{`Preview`}
						</a>
					</>}

					<button type="button" className="btn btn-danger file-selector__remove" onClick={() => this.updateValue(null,null)}>
						{`Remove`}
					</button>
				</>}
			</div>

			{this.props.name && (
				// Also contains a hidden input field containing the value
				<input type="hidden" value={currentRef} name={this.props.name} id={this.props.id} />
			)}
		</div>;
	}

}