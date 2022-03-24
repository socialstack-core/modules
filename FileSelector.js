import Loop from 'UI/Loop';
import Modal from 'UI/Modal';
import Image from 'UI/Image';
import Uploader from 'UI/Uploader';
import omit from 'UI/Functions/Omit';
import getRef from 'UI/Functions/GetRef';
import IconSelector from 'UI/FileSelector/IconSelector';
import Dropdown from 'UI/Dropdown';

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


/**
 * Select a file from a users available uploads, outputting a ref.
 * You can use <Input type="file" .. /> to obtain one of these.
 */
export default class FileSelector extends React.Component {

	constructor(props) {
		super(props);

		this.state = {
			ref: props.value || props.defaultValue
		};

		this.closeModal = this.closeModal.bind(this);
	}

	newId() {
		lastId++;
		return `fileselector${lastId}`;
	}

	updateValue(newRef) {
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

	render() {
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

		return <div className="file-selector">

			{/* upload browser */}
			<Modal
				isExtraLarge
				title={"Select an Upload"}
				className={"image-select-modal"}
				buttons={[
					{
						label: "Close",
						onClick: this.closeModal
					}
				]}
				onClose={this.closeModal}
				visible={this.state.modalOpen}
			>

				<Loop className="file-selector__grid" over="upload/list" filter={{ sort: { field: 'CreatedUtc', direction: 'desc' } }} paged>
					{
						entry => {
							return <>
								<button title={entry.originalName} type="button" className="btn file-selector__item" onClick={() => this.updateValue(entry)}>
									<div className="file-selector__preview">
										{entry.isImage && (
											<Image fileRef={entry.ref} size={256} />
										)}
									</div>
									<span className="file-selector__name">
										{entry.originalName}
									</span>
								</button>
							</>;
						}
					}
				</Loop>

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
						this.updateValue(icon);
					}
				}
			/>

			{/* upload */}
			<Uploader
				currentRef={currentRef}
				originalName={originalName}
				id={this.props.id || this.newId()}
				isPrivate={this.props.isPrivate}
				maxSize={this.props.maxSize}
				onUploaded={
					file => this.updateValue(file)
				} />

			{/* options (browse, preview, remove) */}
			<div className="file-selector__options">
				{!this.props.browseOnly && <>

					{this.props.iconOnly ? <>
						<button type="button" className="btn btn-primary file-selector__select" onClick={() => this.setState({ iconModalOpen: true })}>
							Select icon
						</button>
					</> : <>
						<Dropdown label="Select" variant="primary" className="file-selector__select">
							<li>
								<button type="button" className="btn dropdown-item" onClick={() => this.showModal()}>
									From uploads
								</button>
							</li>
							<li>
								<button type="button" className="btn dropdown-item" onClick={() => this.setState({ iconModalOpen: true })}>
									From icons
								</button>
							</li>
						</Dropdown>
					</>}

				</>}
				{hasRef && <>

					{!getRef.isIcon(currentRef) && <>
						<a href={getRef(currentRef, { url: true })} alt={filename} className="btn btn-primary file-selector__link" target="_blank" rel="noopener noreferrer">
							Preview
						</a>
					</>}

					<button type="button" className="btn btn-danger file-selector__remove" onClick={() => this.updateValue(null)}>
						Remove
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