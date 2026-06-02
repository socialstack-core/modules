import { useEffect, useRef, useState } from "react";
import Input from 'UI/Input';
import Modal from 'UI/Modal';
import Form from 'UI/Form';
import Html from 'UI/Html';
import Button from 'UI/Button';

interface EditorProps {
	readonly?: boolean,
	value?: string,
	defaultValue?: string,
	hideLabel?: boolean,
	label?: React.ReactNode,
	name?: string
}

type Faq = {
	/**
	 * The question, an as-is string.
	 */
	q: string,
	/**
	 * The answer, a HTML string.
	 */
	a: string,
	/**
	 * A non-global number representing a unique FAQ in this array.
	 */
	id: uint
};

type FaqForm = {
	/**
	 * The question, an as-is string.
	 */
	faqQuestion: string,
	/**
	 * The answer, a HTML string.
	 */
	faqAnswer: string,
	/**
	 * A non-global number representing a unique FAQ in this array.
	 */
	id: uint
};

const Editor: React.FC<EditorProps> = (props) => {
	const readonly = props.readonly || false;

	const [value, setValue] = useState<Faq[]>(() => {
		var initValString = (props.value || props.defaultValue || '');
		var initValue = initValString ? JSON.parse(initValString) : [];
		return initValue;
	});
	const [entityToEdit, setEntityToEdit] = useState<Faq | undefined>();
	const showModal = !!entityToEdit;

	const onRemove = (faq: Faq) => {
		var newValue = value.filter((v:Faq) => v != faq);
		setValue(newValue);
	};

	const nextId = () => {
		if (!value || !value.length) {
			return 1;
		}

		let currentMax = 0;
		value.forEach((entry : Faq) => {
			if (entry.id > currentMax) {
				currentMax = entry.id;
			}
		});

		return currentMax + 1;
	};

	return <div className="admin-faq-editor">
		{props.label && !props.hideLabel && (
			<label className="form-label">
				{props.label}
			</label>
		)}

		<table className="table">
			<thead>
				<tr>
					<th>
						{`Question`}
					</th>
					<th>
						{`Answer`}
					</th>
					<th colSpan={2}>
						{`Actions`}
					</th>
				</tr>
			</thead>
			<tbody>
				{
					value.map((faq, index) => {
						return <tr>
							<td>
								{faq.q}
							</td>
							<td>
								<Html>{faq.a}</Html>
							</td>
							{!readonly &&
								<>
									<td>
										<Button sm outlined className="btn-entry-select-action btn-view-entry" title={`Edit`}
											onClick={e => {
												e.preventDefault();
												setEntityToEdit(faq);
											}}>
											<i className="fal fa-fw fa-edit"></i> <span>{`Edit`}</span>
										</Button>
									</td>
									<td>
										<Button sm outlined variant="danger" className="btn-entry-select-action btn-remove-entry" title={`Remove`}
											onClick={e => {
												e.preventDefault();
												onRemove(faq);
											}}>
											<i className="fal fa-fw fa-times"></i> <span>{`Remove`}</span>
										</Button>
									</td>
								</>
							}
						</tr>
					})
				}
			</tbody>
		</table>
		<footer className="admin-multiselect__footer">
			{!readonly &&
				<Button sm outlined className="btn-entry-select-action btn-new-entry new-faq-button"
					onClick={e => {
						e.preventDefault();
						setEntityToEdit({
							id: 0,
							q: '',
							a: ''
						} as Faq);
					}}
				>
					<i className="fal fa-fw fa-plus"></i> {`New FAQ`}
				</Button>
			}
		</footer>
		<input type="hidden" name={props.name} value={value && value.length ? JSON.stringify(value) : ''} />
		{showModal &&
			<Modal
				title={entityToEdit.id ? `Edit FAQ` : `Create New FAQ`}
				visible
				isExtraLarge
				onClose={() => {
					setEntityToEdit(undefined);
				}}
			>
				<Form
					action={(entity : FaqForm) => {
						const newFaq : Faq = {
							q: entity.faqQuestion,
							a: entity.faqAnswer,
							id: entityToEdit.id || nextId() as uint
						};
						
						let existingIndex = -1;

						if (entityToEdit.id) {
							// Attempt to find its index.
							for (var i = 0; i < value.length; i++) {
								if (entityToEdit.id == value[i].id) {
									existingIndex = i;
									break;
								}
							}
						}

						if (existingIndex == -1) {
							// Insert the new one
							setValue([...value, newFaq]);
						} else {
							// Replace at that index
							const newValue = [...value];
							newValue[existingIndex] = newFaq;
							setValue(newValue);
						}

						// Clear editing one
						setEntityToEdit(undefined);

						return Promise.resolve(newFaq);
					}}
					submitLabel={entityToEdit.id ? `Update` : `Create`}
				>
					<Input type="text" name="faqQuestion" defaultValue={entityToEdit?.q} label={`Question`} validate={['Required']} />
					<Input type="html" name="faqAnswer" defaultValue={entityToEdit?.a} label={`Answer`} validate={['Required']} required />
				</Form>
			</Modal>
		}
	</div>;
};


export default Editor;