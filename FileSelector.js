import Loop from 'UI/Loop';
import Modal from 'UI/Modal';
import Alert from 'UI/Alert';
import Image from 'UI/Image';
import Uploader from 'UI/Uploader';
import omit from 'UI/Functions/Omit';
import getRef from 'UI/Functions/GetRef';
import IconSelector from 'UI/FileSelector/IconSelector';

var inputTypes = global.inputTypes = global.inputTypes || {};

inputTypes.ontypefile = inputTypes.ontypeimage = function(props, _this){
	return (
		<FileSelector 
			id={props.id || _this.fieldId}
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
	
	constructor(props){
		super(props);
		
		this.state = {
			editing: false
			
		};
		this.closeModal = this.closeModal.bind(this);
	}
	
	updateValue(newRef) {
		console.log("updateValue");
		console.log(newRef);
		if(!newRef){
			newRef = '';
		}
		
		if(newRef.uploadRef){
			// Accept upload objects also.
			newRef = newRef.uploadRef;
		}else if(newRef.ref){
			newRef = newRef.ref;
		}
		
		this.props.onChange && this.props.onChange({target: {value: newRef}});
		
		this.setState({
			value: newRef,
			modalOpen: false,
			editing: false
		});
	}
	
	showRef(ref){
		// Check if it's an image/ video/ audio file. If yes, a preview is shown. Otherwise it'll be a preview link.
		var canShowImage = getRef.isImage(ref);
		
		return <a href={getRef(ref, {url: true})} alt={ref} target={'_blank'}>
			{
				canShowImage ? (
					<Image fileRef={ref} size={256} alt={ref}/>
				) : 'View selected file'
			}
		</a>;
		
	}
	
	showModal(){
		this.setState({modalOpen: true});
	}
	
	closeModal(){
		this.setState({modalOpen: false});
	}
	
	render() {
		var currentRef = this.props.value || this.props.defaultValue;
		
		if(this.state.value !== undefined){
			currentRef = this.state.value;
		}
		
		var hasRef = currentRef && currentRef.length;
		
		return <div className="file-selector">
			<Modal
				isExtraLarge
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
				<table>
					<tr>
						<th>Name</th>
						<th></th>
					</tr>
					<Loop over="upload/list" filter={{sort: {field: 'CreatedUtc', direction: 'desc'}}} asRaw paged>
					{
						entry => <tr onClick={() => this.updateValue(entry)}>
							<td width='85%'>
								{entry.originalName}
							</td>
							<td width='15%' align='right' className="break-word">
								{entry.isImage && (
									<Image fileRef={entry.ref} size={128} />
								)}
							</td>
						</tr>
					}
					</Loop>
					</table>
			</Modal>
			<IconSelector 
				visible = {this.state.iconModalOpen} 
				onClose = {() => {
					this.setState({iconModalOpen: false})
				}}
				onSelected = {
					icon => {
						console.log("onSelected");
						console.log(icon);
						this.updateValue(icon)
					}
				}
			/>

			{this.state.editing ? (
				<>
				{/*
				<div>
					<span className="btn btn-secondary" onClick={() => this.showModal()}>Select from uploads</span> or <Uploader onUploaded={
						file => this.updateValue(file)
					}/>
				</div>
				*/}
				<div class="btn-group btn-group-toggle canvas-editor-view file-selector-btn-group" data-toggle="buttons">
					<label class="btn btn-sm btn-primary">
						<input type="radio" name="fileSelectorOptions" id="selectUploads" autocomplete="off"
							checked={false} onChange={() => {
								this.showModal()
							}} />
						<i class="far fa-images"></i>
						Select from uploads
					</label>
					<label class="btn btn-sm btn-primary">
						<input type="radio" name="fileSelectorOptions" id="uploadNew" autocomplete="off"
							checked={false} onChange={() => {
								this.setState({

								});
							}} />
						<i class="far fa-upload"></i>
						<Uploader onUploaded={
							file => this.updateValue(file)
						}/>
					</label>
					<label class="btn btn-sm btn-primary">
						<input type="radio" name="fileSelectorOptions" id="selectIcons" autocomplete="off"
							checked={false} onChange={() => {
								this.setState({
									iconModalOpen: true
								});
							}} />
						<i class="far fa-icons"></i>
						Select from icons
					</label>
				</div>
				</>
			) : (
				<div>
					{hasRef && (currentRef.endsWith('.webp') || currentRef.endsWith('.avif')) && <p>
						<Alert type='info'>Format requires manual resizing</Alert>
						</p>
					}
					{hasRef ? this.showRef(currentRef) : 'None selected'}
					&nbsp;
					<div className="btn btn-secondary" onClick={() => this.setState({editing: true})}>Change</div>
					<div className="btn btn-danger delete-file-btn" onClick={() => this.updateValue(null)}>Remove</div>
				</div>
			)}
			{this.props.name && (
				/* Also contains a hidden input field containing the value */
				<input type="hidden" value={currentRef} name={this.props.name} id={this.props.id} />
			)}
		</div>;
	}
	
}