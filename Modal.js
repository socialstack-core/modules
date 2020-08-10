
/*
* A popup modal. Usage is e.g.:

<Modal
	buttons={[
		{
			label: "Close",
			onClick: this.closeModal
		}
	]}
	onClose={this.closeModal}
	visible={this.state.modalOpen}
>
	This is the modals content.
</Modal>

*/
export default class Modal extends React.Component {

    backdropClassName() {
        let classes = 'modal-backdrop show';
        return classes;
    }
	
    modalClassName() {
        let classes = this.props.className || "";
		classes += " modal";
        if (this.props.fade) {
            classes += " fade";
        }else{
			classes += " show";
		}
		
        return classes;
    }

    modalDialogClassName() {
        let classes = 'modal-dialog show';

        if (this.props.isCentred) {
            classes+=' modal-dialog-centered';
		}

		if (this.props.isScrollable) {
			classes += ' modal-dialog-scrollable';
		}
		
        if (this.props.isSmall) {
            classes+=' modal-sm';
        }

        if (this.props.isLarge) {
            classes+=' modal-lg';
        }

        if (this.props.isExtraLarge) {
            classes+=' modal-xl';
        }

        if (this.props.customClass) {
            classes+=' ' + this.props.customClass;
        }

        return classes;
    }

    closeModal() {
        this.props.onClose && this.props.onClose();
    }
	
    render() {
		if(!this.props.visible){
			return null;
		}
		
        return [
			this.props.noBackdrop ? null : <div className={this.backdropClassName()} onClick={() => this.closeModal()}></div>,
			<div className={this.modalClassName()} tabIndex="-1" role="dialog">
				<div className={this.modalDialogClassName()} role="document">
					<div className="modal-content">
						<div className="modal-header">
							<h5 className="modal-title">{this.props.title}</h5>
							<button type="button" className="close" data-dismiss="modal" aria-label="Close"
									onClick={() => this.closeModal()}>
								<span aria-hidden="true">&times;</span>
							</button>
						</div>
						<div className="modal-body">
							{this.props.children}
						</div>
						<div className="modal-footer">
							{this.props.footer ? this.props.footer() : this.renderButtons()}
						</div>
					</div>
				</div>
			</div>
        ];
    }
	
	renderButtons(){
		if(!this.props.buttons){
			return null;
		}
		
		return this.props.buttons.map(buttonInfo => {
			
			return (
				<button 
					type="button"
					className={"btn " + (buttonInfo.className || "btn-primary")}
					onClick={() => buttonInfo.onClick()}
					{...(buttonInfo.props || {})}
				>
					{buttonInfo.label}
				</button>
			);
			
		});
		
	}
}
