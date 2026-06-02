import Form from 'UI/Form';
import Input from 'UI/Input';
import Modal from 'UI/Modal';
import customContentTypeSelectOptionApi, { CustomContentTypeSelectOption } from 'Api/CustomContentTypeSelectOption';
import Loop from 'UI/Loop';
import AutoForm from 'Admin/AutoForm';
import Button from 'UI/Button';
import Icon from 'UI/Icon';
import { useState, useRef } from 'react';

interface CustomFieldSelectFormProps {
	fieldId: int;
}

const CustomFieldSelectForm: React.FC<CustomFieldSelectFormProps> = (props) => {
	const [newOptionInput, setNewOptionInput] = useState <HTMLInputElement | null>(null);
	const [showCreateOrEditModal, setShowCreateOrEditModal] = useState(false);
	const [entityToEdit, setEntityToEdit] = useState<CustomContentTypeSelectOption | null>(null);

	const deleteOption = (id: int) => {
		customContentTypeSelectOptionApi.delete(id).then(() => {
			setShowCreateOrEditModal(false);
		}).catch((e: any) => {
			console.error(e);
		});
	};

	return <div className="custom-field-select-form">
		<Loop
			over={customContentTypeSelectOptionApi}
			filter={
				{
					query: 'CustomContentTypeFieldId=?',
					args: [props.fieldId],
					sort: { field: 'order' }
				}
			}
		>
			{
				(option) => {
					return <div className="custom-field-select-form--option" key={option.id}>
						<span className="option-value">{option.value}</span>
						<div className="option-buttons">
							<Button className="option-edit"
								onClick={e => {
									e.preventDefault();
									setEntityToEdit(option);
									setShowCreateOrEditModal(true);
								}}>
								<Icon regular fixedWidth type="fa-edit" />
							</Button>
							<Button className="option-delete"
								onClick={e => {
									e.preventDefault();
									deleteOption(option.id);
								}}>
								<Icon type="fa-trash" />
							</Button>
						</div>
						<span className="option-order">
							{`Order`}: {option.order}
						</span>
					</div>
				}
			}
		</Loop>
		<Form
			action={customContentTypeSelectOptionApi.create}
			onValues={values => {
				values.customContentTypeFieldId = props.fieldId;
				return values;
			}}
			onSuccess={(response: any) => {
				if (newOptionInput) {
					newOptionInput.value = "";
				}
			}}
		>
			<Input type="text" name="value" label={`New Option`} validate={['Required']} onInputRef={el => setNewOptionInput(el as HTMLInputElement)} />
			<Input type="submit" label={`Create Option`} />
		</Form>
		{showCreateOrEditModal &&
			<Modal
				title={entityToEdit ? `Edit Option` : `Create New Option`}
				onClose={() => {
					setShowCreateOrEditModal(false);
					setEntityToEdit(null);
				}}
				visible
				isExtraLarge
			>
				<AutoForm
					contentType={"customContentTypeSelectOption"}
					singular={`Options`}
					plural={`Options`}
					content={entityToEdit || undefined}
					onActionComplete={(entity: any) => {
						setShowCreateOrEditModal(false);
						setEntityToEdit(null);
					}} />
			</Modal>
		}
	</div>;
};

export default CustomFieldSelectForm;
