import Loop from 'UI/Loop';
import CustomFieldForm from 'Admin/CustomFieldEditor/CustomFieldForm';
import Modal from 'UI/Modal';
import webRequest from 'UI/Functions/WebRequest';

export default class CustomFieldEditor extends React.Component {
	constructor(props) {
		super(props);

		this.state = {
			showFieldModal: false,
			showConfirmDeleteModal: false
		}

		this.renderEntry = this.renderEntry.bind(this);
		this.renderFooter = this.renderFooter.bind(this);
		this.deleteField = this.deleteField.bind(this);
	}

	onFieldCreate() {
		this.setState({ showFieldModal: false });
	}

	renderHeader() {
		return <>
			<th className="custom-field-editor__name">{`Name`}</th>
			<th className="custom-field-editor__nickname">{`Nickname`}</th>
			<th className="custom-field-editor__type">{`Data Type`}</th>
			<th className="custom-field-editor__localised">{`Localised`}</th>
			<th className="custom-field-editor__order">{`Order`}</th>
			<th className="custom-field-editor__actions"></th>
		</>;
	}

	renderColgroups() {
		return <>
			<col className='col__name'></col>
			<col className='col__nickname'></col>
			<col className='col__type'></col>
			<col className='col__bool'></col>
			<col className='col__order'></col>
			<col className='col__actions'></col>
		</>;
	}

	renderEntry(entry) {
		var order = entry.order ? entry.order : null;

		if (entry.dataType == "entitylist") {
			order = "NA"
		}

		return <>
			<td className="custom-field-editor__name">{entry.name}</td>
			<td className="custom-field-editor__nickname">{entry.nickName}</td>
			<td className="custom-field-editor__type">{entry.dataType}</td>
			<td className="custom-field-editor__localised">
				<i className={entry.localised ? 'fa fa-fw fa-check' : 'fa fa-fw fa-times'}></i>
			</td>
			<td className="custom-field-editor__order">{order}</td>
			<td className="custom-field-editor__actions">
				<button type="button" className="btn btn-sm btn-outline-primary" onClick={() => this.setState({ showFieldModal: entry })}>
					<i className="far fa-fw fa-edit"></i>
					{`Edit`}
				</button>
				<button type="button" className="btn btn-sm btn-outline-danger" onClick={() => this.setState({ showConfirmDeleteModal: entry })}>
					<i className="far fa-fw fa-trash"></i>
					{`Delete`}
				</button>
			</td>
		</>;
	}

	renderFooter() {
		return (
			<td colspan="5" className="custom-field-editor__footer">
				<button type="button" className="btn btn-primary" onClick={() => this.setState({ showFieldModal: true })}>
					{`Add New Field`}
				</button>
			</td>
		);
    }

	renderEmpty() {
		return <table className="table">
			<thead>
				<tr>
					{this.renderHeader()}
				</tr>
			</thead>
			<colgroup>
				{this.renderColgroups()}
			</colgroup>
			<tbody>
				<tr>
					<td colspan="5" className="custom-field-editor__empty-message">
						{`No fields defined - click "Add new field" to create fields`}
					</td>
				</tr>
			</tbody>
			<tfoot>
				<tr>
					{this.renderFooter()}
				</tr>
			</tfoot>
		</table>;
    }

	deleteField(id) {

		if (this.state.showConfirmDeleteModal == false) {
			return;
		}
		
		webRequest("customContentTypeField/" + id, undefined, { method: "delete" }).then(() => {
			this.setState({ showConfirmDeleteModal: false });
		}).catch(e => {
			console.error(e);
		});
	}

	render() {

		if (!this.props.currentContent || this.props.currentContent.type !== "CustomContentType") {
			console.log("CustomFieldEditor only supports the CustomContentType module");
			return null;
		}

		return <div className="mb-3">
			<label className="form-label" htmlFor="custom-field-list">
				{`Fields`}
			</label>
			<Loop asTable live
				over='customContentTypeField'
				filter={
					{
						where: {
							customContentTypeId: this.props.currentContent.id,
							deleted: false
						}
					}
				}
				orNone={() => this.renderEmpty()}>
				{[
					this.renderHeader,
					this.renderColgroups,
					this.renderEntry,
					this.renderFooter
				]}
			</Loop>
			{this.state.showConfirmDeleteModal && <>
				<Modal
					visible
					className="custom-field-editor__modal"
					title={`Delete Custom Content Type Field`}
					onClose={() => this.setState({ showConfirmDeleteModal: false })}
				>
					<p>{`This will remove custom field "${this.state.showConfirmDeleteModal.name}".`}</p>
					<p>{`Are you sure you wish to do this?`}</p>

					<footer className="custom-field-editor__modal-footer">
						<button type="button" className="btn btn-outline-primary cancelButton" onClick={() => { this.setState({ showConfirmDeleteModal: false }) }}>
							{`Cancel`}
						</button>
						<button type="button" className="btn btn-danger confirmButton" onClick={() => { this.deleteField(this.state.showConfirmDeleteModal.id), this.setState({ showConfirmDeleteModal: false }) }}>
							{`Confirm`}
						</button>
					</footer>

				</Modal>
			</>}
			{this.state.showFieldModal && <>
				<Modal
					visible
					className="custom-field-editor__modal"
					title={this.state.showFieldModal == true ? `Add New Field` : `Edit Field`}
					onClose={() => this.setState({ showFieldModal: false })}
				>
					{this.state.showFieldModal == true && <>
						<CustomFieldForm customContentTypeId={this.props.currentContent.id} isFormField={this.props.currentContent.isForm}
							onCreate={() => this.onFieldCreate()} onCancel={() => { this.setState({ showFieldModal: false }) }} />
					</>}

					{this.state.showFieldModal != true && <>
						<CustomFieldForm key={this.state.showFieldModal.id} field={this.state.showFieldModal} customContentTypeId={this.props.currentContent.id} isFormField={this.props.currentContent.isForm}
							onCancel={() => { this.setState({ showFieldModal: false }) }} />
					</>}

				</Modal>
			</>}
		</div>;
	}

}
