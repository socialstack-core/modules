import Table from 'UI/Table';
import Button from 'UI/Button';
import CustomFieldForm from 'Admin/CustomFieldEditor/CustomFieldForm';
import Modal from 'UI/Modal';
import ConfirmDialog from 'UI/Dialog/ConfirmDialog';
import customContentTypeFieldApi from 'Api/CustomContentTypeField';
import { useState } from 'react';

interface CustomFieldEditorProps {
	currentContent?: {
		id: int;
		type: string;
		isForm: boolean;
	};
}

const CustomFieldEditor: React.FC<CustomFieldEditorProps> = (props) => {
	const [showFieldModal, setShowFieldModal] = useState<any>(false);
	const [showConfirmDeleteModal, setShowConfirmDeleteModal] = useState<any>(false);

	const onFieldCreate = () => {
		setShowFieldModal(false);
	};

	const renderHeader = () => {
		return <>
			<th className="custom-field-editor__name">{`Name`}</th>
			<th className="custom-field-editor__nickname">{`Nickname`}</th>
			<th className="custom-field-editor__type">{`Data Type`}</th>
			<th className="custom-field-editor__localised">{`Localised`}</th>
			<th className="custom-field-editor__order">{`Order`}</th>
			<th className="custom-field-editor__actions"></th>
		</>;
	};

	const renderColgroups = () => {
		return <>
			<col className='col__name'></col>
			<col className='col__nickname'></col>
			<col className='col__type'></col>
			<col className='col__bool'></col>
			<col className='col__order'></col>
			<col className='col__actions'></col>
		</>;
	};

	const renderEntry = (entry: any) => {
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
				<Button sm outlined onClick={() => setShowFieldModal(entry)}>
					<i className="far fa-fw fa-edit"></i>
					{`Edit`}
				</Button>
				<Button sm outlined variant="danger" onClick={() => setShowConfirmDeleteModal(entry)}>
					<i className="far fa-fw fa-trash"></i>
					{`Delete`}
				</Button>
			</td>
		</>;
	};

	const renderFooter = () => {
		return (
			<td colSpan={5} className="custom-field-editor__footer">
				<Button onClick={() => setShowFieldModal(true)}>
					{`Add New Field`}
				</Button>
			</td>
		);
	};

	const renderEmpty = () => {
		return <table className="table">
			<thead>
				<tr>
					{renderHeader()}
				</tr>
			</thead>
			<colgroup>
				{renderColgroups()}
			</colgroup>
			<tbody>
				<tr>
					<td colSpan={5} className="custom-field-editor__empty-message">
						{`No fields defined - click "Add new field" to create fields`}
					</td>
				</tr>
			</tbody>
			<tfoot>
				<tr>
					{renderFooter()}
				</tr>
			</tfoot>
		</table>;
	};

	const deleteField = (id: number) => {
		if (showConfirmDeleteModal == false) {
			return;
		}

		customContentTypeFieldApi.delete(id as int).then(() => {
			setShowConfirmDeleteModal(false);
		}).catch((e: any) => {
			console.error(e);
		});
	};

	if (!props.currentContent || props.currentContent.type !== "CustomContentType") {
		console.log("CustomFieldEditor only supports the CustomContentType module");
		return null;
	}

	return <div className="mb-3">
		<label className="form-label" htmlFor="custom-field-list">
			{`Fields`}
		</label>
		<Table
			over={customContentTypeFieldApi}
			filter={
				{
					query:'CustomContentTypeId=? and Deleted=?',
					args: [props.currentContent.id, false],
					sort: { field: 'order' }
				}
			}
			onHeader={renderHeader}
			colGroups={renderColgroups}
			onFooter={renderFooter}
			orNone={() => renderEmpty()}>
			{renderEntry}
		</Table>

		{showConfirmDeleteModal && <>
			<ConfirmDialog variant="primary"
				isOpen={showConfirmDeleteModal}
				title={`Delete Custom Content Type Field`}
				onClose={() => setShowConfirmDeleteModal(false)}
				confirmText={`Confirm`}
				confirmCallback={() => {
					deleteField(showConfirmDeleteModal.id), setShowConfirmDeleteModal(false);
				}}>
				<p>{`This will remove custom field "${showConfirmDeleteModal.name}".`}</p>
				<p>{`Are you sure you wish to do this?`}</p>
			</ConfirmDialog>
		</>}

		{showFieldModal && <>
			<Modal
				visible
				className="custom-field-editor__modal"
				title={showFieldModal == true ? `Add New Field` : `Edit Field`}
				onClose={() => setShowFieldModal(false)}
			>
				{showFieldModal == true && <>
					<CustomFieldForm customContentTypeId={props.currentContent.id} isFormField={props.currentContent.isForm}
						onCreate={() => onFieldCreate()} onCancel={() => { setShowFieldModal(false) }} />
				</>}

				{showFieldModal != true && <>
					<CustomFieldForm key={showFieldModal.id} field={showFieldModal} customContentTypeId={props.currentContent.id} isFormField={props.currentContent.isForm}
						onCancel={() => { setShowFieldModal(false) }} />
				</>}

			</Modal>
		</>}
	</div>;
};

export default CustomFieldEditor;
