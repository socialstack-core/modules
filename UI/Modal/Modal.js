import getRef from 'UI/Functions/GetRef';
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
var titleId = 1;

export default class Modal extends React.Component {

	constructor(props) {
		super(props);
		this.newTitleId();
	}

	componentDidMount() {
		this.props.onOpen && this.props.onOpen();
    }

	newTitleId() {
		this.modalTitleId = 'modal_title_' + (titleId++);
	}

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

		// NB: modals centred on-screen by default; use isNotCentred prop to disable
        if (!this.props.isNotCentred) {
            classes+=' modal-dialog-centered';
		}

		// NB: modals scrollable by default; use isNotScrollable prop to disable
		if (!this.props.isNotScrollable) {
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

		if (this.props.hideSelector) {
			const hideElements = Array.prototype.slice.apply(
				document.querySelectorAll(this.props.hideSelector)
			);

			hideElements.forEach((element) => {
				element.classList.remove("hidden-by-modal");
			});

		}

        this.props.onClose && this.props.onClose();
    }
	
    render() {
		if(!this.props.visible){
			return null;
		}

		if (this.props.hideSelector) {
			const hideElements = Array.prototype.slice.apply(
				document.querySelectorAll(this.props.hideSelector)
			);

			hideElements.forEach((element) => {
				element.classList.add("hidden-by-modal");
			});

		}

		var style = {};
		if(this.props.backgroundImageRef) {
			style.backgroundImage = "url("+ getRef(this.props.backgroundImageRef, {url: true}) +")"
			style.height= "690px"; /* You must set a specified height */
  			style.backgroundPosition= "center"; /* Center the image */
			style.backgroundRepeat= "no-repeat"; /* Do not repeat the image */
			style.backgroundSize= "cover";
		}

		var closeClass = this.props.closeIcon ? "close btn-close custom-icon" : "close btn-close";
		var closeIconClass = this.props.closeIcon ? "close-icon custom-icon-content" : "close-icon";

        return [
			this.props.noBackdrop ? null : <div className={this.backdropClassName()} onClick={() => this.closeModal()}></div>,
			<div className={this.modalClassName()} tabIndex="-1" role="dialog" aria-labelledby={this.modalTitleId} data-theme={this.props['data-theme'] || 'modal-theme'}
				onKeyUp={this.props.noClose ? undefined : (e) => {
					if (e.key === "Escape") {
						this.closeModal();
					}
				}}
			onTouchStart={e => e.stopPropagation()}>
				<div className={this.modalDialogClassName()} role="document">
					<div className="modal-content" style = {style}>
						{this.props.noHeader ? <></> : <div className="modal-header">
							<div className="modal-title" id={this.modalTitleId}>{typeof this.props.title === 'string' ? <h5>{this.props.title}</h5> : this.props.title}</div>
							{this.props.noClose ? <></> : <button type="button" className={closeClass} data-dismiss="modal" aria-label={`Close`}
								onClick={() => this.closeModal()}>
								<span aria-hidden="true" className={closeIconClass}>
										{!this.props.closeIcon && (
											<>
												&times;
											</>
										)}
										{this.props.closeIcon && (
											<>
												{this.props.closeIcon}
											</>
										)}
								</span>
							</button>}
						</div>}
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

Modal.propTypes = {
	noHeader: 'bool',
	noBackdrop: 'bool',
	noClose: 'bool',
	visible: 'bool',
	isExtraLarge: 'bool',
	backgroundImageRef: 'string',
	children: true
	
}

