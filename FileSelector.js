import Loop from 'UI/Loop';
import Modal from 'UI/Modal';
import Image from 'UI/Image';
import Uploader from 'UI/Uploader';

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
		if(!newRef){
			newRef = '';
		}
		
		if(newRef.uploadRef){
			// Accept upload objects also.
			newRef = newRef.uploadRef;
		}else if(newRef.ref){
			newRef = newRef.ref;
		}
		
		this.setState({
			value: newRef,
			modalOpen: false,
			editing: false
		});
	}
	
	showModal(){
		this.setState({modalOpen: true});
	}
	
	closeModal(){
		this.setState({modalOpen: false});
	}
	
	render() {
		var currentRef = this.state.value || this.props.value || this.props.defaultValue;
		
		return [
			<Modal
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
				<Loop over="uploader/list" inline>
				{
					entry => {
						return <Image fileRef={entry.ref} size={64} alt={entry.name} onClick={() => {
							
							this.updateValue(entry);
							
						}}/>;
					}
				}
				</Loop>
			</Modal>,
			this.state.editing ? (
				<div>
					<span className="btn btn-secondary" onClick={() => this.showModal()}>Select from uploads</span> or <Uploader onUploaded={
						file => this.updateValue(file)
					}/>
				</div>
			) : (
				<div>
					{currentRef && currentRef.length ? <Image fileRef={currentRef} size={64} /> : 'None selected'}
					&nbsp;
					<div className="btn btn-secondary" onClick={() => this.setState({editing: true})}>Change</div>
				</div>
			),
			this.props.name ? (
				/* Also contains a hidden input field containing the value */
				<input type="hidden" value={currentRef} name={this.props.name} id={this.props.id} />
			) : null
		];
	}
	
}