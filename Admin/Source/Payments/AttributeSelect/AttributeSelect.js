// ==========================
// TODOS
// ==========================
// 1. Replace this with typescript :)

// =========================
// React Imports
// =========================
import {createRef, useState} from "react";

// ===========================
// UI Imports
// ===========================
import Search from 'UI/Search';
import Modal from 'UI/Modal';
import Canvas from 'UI/Canvas';
import Image from 'UI/Image';
import Link from 'UI/Link';
import * as fileRef from 'UI/FileRef';
import Loading from "UI/Loading";
import Button from "UI/Button";

// ===========================
// API Imports
// =========================== 
import productAttributeValueApi, { ProductAttributeValue } from 'Api/ProductAttributeValue';
import productAttributeApi from "Api/ProductAttribute";
import productAttribute from "Api/ProductAttribute";
import Input from "UI/Input";
import Loop from "UI/Loop";
import Alert from "UI/Alert";

// ===========================
// JSDoc
// ===========================
/**
 * Attribute selector
 * @typedef {Object} ObjectProps
 * @property {any} [value] - Optional value
 * @property {any} [defaultValue] - Optional default value
 * @property {number[]} [requiredAttributes] - Optional list of required attributes
*/

/**
 * SpecifyModalProps
 * @typedef {Object} SpecifyModalProps
 * @property {ProductAttribute} attribute
 * @property {ProductAttributeValue} value
 * @property {string} displayFieldName
 * @property {(value: ProductAttributeValue) => void} onChange
 */

// ===========================
// Caching
// ===========================
/**
 * A reference to the AutoForm component.
  * @type {AutoForm | null}
 */	
let AutoForm = null;

// ===========================
// Export
// ===========================
/**
 * A general use "multi-selection"; primarily used for tags and categories.
 */
export default class AttributeSelect extends React.Component {
	
	fieldRef = null;
	
	/**
	 * Admin Product Attribute Select component.
	 * @param {ObjectProps} props
	 */
    constructor(props) {
		
		// the super constructor
		// is called first ofc. 
        super(props);
		
		this.fieldRef = createRef(null);
		
		// a simple flag that gets passed to state, to trigger
		// the loading of the initVal attributes by their
		// respective IDs. 
		// (defaults to false).
		let mustLoad = false;
		
		/**
		    this must resolve to a numeric array int[], uint[] etc...
		 	@type {ProductAttributeValue[]}
		*/
		let initVal = (props?.value || props?.defaultValue || []).filter(t => t!=null);
		
		// check length & the type, number could also be a float
		// so, lets add a guard condition where we
		// round the number, and warn the developer
		// in the console.
		if (initVal.length) {
			
			// Instead of the previous map function,
			// we can iterate every value to properly
			// check it's the expected format, and that 
			initVal = initVal.map((value, idx) => {
				
				// when the value hasn't been loaded
				// it exists purely as a number, it needs loading.
				// we now know it's a number,
				// but we don't know if it's a whole
				// integer, or a float. 
				if (typeof value === 'number') {
					
					// if it's an integer, wicked
					// we can return the value.
					if (Number.isInteger(value)) {
						
						// mustLoad flag now flips
						mustLoad = true;
						
						// return the ID to load.
						/**
						 * @type {ProductAttributeValue}
						 */
						return {
							id: value
						}
					}
				}
				
				// if its loaded, no need to reload
				if (value.id) {
					return value;
				}

				console.warn('[AttributeSelect][props] - Expected initVal.(value|defaultValue) index ' + idx + ' to be an integer. You supplied "' + value + "'" , value);

				// we skip float values, and if it's not a number then 
				return null;
				
			}).filter(Boolean)
		}
		
		// update the initial state. Any state changes after should use this.setState({ /* YOUR STATE HERE */ })
		this.state = {
			/**
			 * @type {ProductAttributeValue[]} initVal
			 */
			value: initVal,
			/**
			 * @type {boolean} mustLoad
			 */
			mustLoad,
			/**
			 * @type {boolean} showCreateOrEditModal
			 */
			showCreateOrEditModal: false,
			/**
			 * 
			 */
			specifyAttributeValue: null
		};

		if (!AutoForm) {
			AutoForm = require("Admin/AutoForm").default;
		}
    }
	
	componentDidMount(){
		if (!this.state.mustLoad) {

			var filter = {
				query: "Id=[?]",
				args: [this.state.value.map(e => e.id)]
			}
			
			productAttributeValueApi.list(filter, [productAttributeValueApi.includes.attribute]).then(response => {
				
				// Loading the values and preserving order:
				var idLookup = {};
				response.results.forEach(r => {idLookup[r.id+''] = r;});

				this.setState({
					mustLoad: false,
					value: this.state.value.map(e => idLookup[e.id+'']).filter(t=>t!=null)
				});
				
			});
		}
	}
	
	componentWillReceiveProps(props){
		if(props.value){
			this.setState({
				value: props.value.filter(t => t!=null)
			});
		}
	}

	remove(entry) {
		var value = this.state.value.filter(t => t!=entry && t!=null);
		this.runChange(value);
	}

	runChange(value) {
		this.setState({
			value
		});
		var e = { target: { value: value.map(e => e.id) }, fullValue: value };
		this.props.onRawChange && this.props.onRawChange(e);
		this.props.onChange && this.props.onChange(e);
	}

    renderResult(result) {
		if (!result.attribute)
		{
			console.error('Orphaned attribute' , result)
			return;
		}
		return (
			<>
				{result.attribute.name}{`:`}&nbsp;{result.value}{result.attribute.units?result.attribute.units:''}
			</>
			);
    }
	throwIfMissingValue()
	{
		const missingValues = [];
		
		this.getRequiredAttributes().forEach((attribute) => {
			if (!this.state.value.find(value => value.productAttributeId == attribute.id)) {
				missingValues.push(attribute);
			}	
		})
		
		if (missingValues.length != 0) {
			
			// focus the element. 
			this.fieldRef?.current?.scrollIntoView({
				behavior: "smooth",
			})
			
			this.setState({
				error: `Missing ${missingValues.length} required attributes: ${missingValues.map(attr => attr.name).join(', ')}`
			})
			
			throw {
				type: 'validation',
				message: `Missing ${missingValues.length} required attributes: ${missingValues.map(attr => attr.name).join(', ')}`
			}
		}
	}

	getRequiredAttributes() {
		// fall back to this.
		return Array.isArray(this.props.requiredAttributes) ? this.props.requiredAttributes : [];
	}
	
	getFieldName() {
		let fieldName = this.props.field;
		
		if (!fieldName) {
			fieldName = 'value';
		}
		return fieldName;		
	}
	
	getDisplayFieldName() {
		let displayFieldName = this.props.displayField || this.getFieldName();
		
		if (displayFieldName.length) {
			displayFieldName = displayFieldName[0].toLowerCase() + displayFieldName.substring(1);
		}
		return displayFieldName;
	}


	/**
	 * Renders the main UI of the AttributeSelect component.
	 *
	 * This method constructs a multi-selection interface for product attributes.
	 * It handles:
	 * - Displaying required attributes (with loading state)
	 * - Displaying currently selected attributes
	 * - Removing attributes
	 * - Adding new attributes via search or smart value creation
	 * - Optional media previews (images/videos)
	 * - Integration with a hidden input for form submission
	 */
	render() {
		// Determine field names for internal logic and display
		let fieldName = this.getFieldName();                // Field to store/retrieve values (default: 'value')
		let displayFieldName = this.getDisplayFieldName();  // Field to display in the UI (default: fieldName)

		// Field to check for media references (images/videos)
		var mediaRefFieldName = 'featureRef';

		// Check if we reached the maximum number of selectable attributes
		var atMax = false;
		if (this.props.max > 0) {
			atMax = (this.state.value.length >= this.props.max);
		}

		// Collect IDs of currently selected attributes for exclusion in search
		let excludeIds = this.state.value.map(a => a.id);

		return (
			<>
				{/* Main wrapper for multi-selection */}
				<div className="admin-multiselect mb-3" ref={this.fieldRef}>

					{/* Optional label with link to attributes */}
					{this.props.label && !this.props.hideLabel && (
						<label className="form-label">
							{this.props.label}{' '}
							<Link href='/en-admin/attribute/'><i className="fa fa-external-link" /></Link>
						</label>
					)}
					{this.state.error && <>
						<br />
						<span className={'validation-error'}>{this.state.error}</span>
					</>}
					{/* List of attribute entries */}
					<ul className="admin-multiselect__entries">

						{/* =========================
                        Required Attributes Section
                        ========================= */}
						{this.getRequiredAttributes().length ? (
							// Render required attributes once loaded
							this.getRequiredAttributes().map((attribute, idx) => {
								// Find existing value for this required attribute
								const existingValue = this.state.value.find(
									entry => entry?.attribute?.id === attribute.id
								);

								// Render the required attribute entry
								return this.renderRequiredAttribute(attribute, idx, existingValue);
							})
						) : null}

						{/* =========================
                        Other Attributes Section
                        ========================= */}
						{this.state.value
							.filter((entry) => {
								// Exclude required attributes from this section
								if (!this.getRequiredAttributes().length) return true;
								return !this.getRequiredAttributes().find(attr => attr == entry.productAttributeId || attr.id == entry.productAttributeId);
							})
							.map((entry, i) => (
								<li key={i + entry.id} className="admin-multiselect__entry">

									{/* Attribute name */}
									<div>
										{entry.attribute ? entry.attribute.name : ''}
									</div>

									{/* Attribute value */}
									<div>
										{displayFieldName.indexOf("Json") !== -1
											? <Canvas>{entry[displayFieldName]}</Canvas>
											: entry[displayFieldName]
										}
										{entry.attribute ? entry.attribute.units : ''}
									</div>

									{/* Entry options (media previews, remove button) */}
									<div className="admin-multiselect__entry-options">
										{mediaRefFieldName && entry[mediaRefFieldName]?.length > 0 &&
											<div className="admin-multiselect__avatar">
												{fileRef.isImage(entry[mediaRefFieldName]) && (
													<Image fileRef={entry[mediaRefFieldName]} size={32} />
												)}
												{fileRef.isVideo(entry[mediaRefFieldName]) && (
													<i className="fa fa-2x far-file"></i>
												)}
											</div>
										}

										{/* Remove button */}
										<button
											className="btn btn-sm btn-outline-danger btn-entry-select-action btn-remove-entry"
											title={`Remove`}
											onClick={() => this.remove(entry)}
										>
											<i className="fal fa-fw fa-times"></i>
											<span className="sr-only">{`Remove`}</span>
										</button>
									</div>
								</li>
							))
						}
					</ul>

					{/* Hidden input for form submission */}
					<input
						type="hidden"
						name={this.props.name}
						ref={ele => {
							this.input = ele;
							if (ele != null) {
								ele.onGetValue = (v, input, e) => {
									if (input !== this.input) {
										return v;
									}
									this.throwIfMissingValue();
									return this.state.value.map(entry => entry.id);
								}
							}
						}}
					/>

					{/* Footer with search / smart value creation */}
					<footer className="admin-multiselect__footer">
						{atMax ? (
							<span className="admin-multiselect__search-max">
                            <i>{`Max of ${this.props.max} added`}</i>
                        </span>
						) : (
							
								<Search
									endpoint={productAttributeValueApi.list}
									includes={[productAttributeValueApi.includes.attribute]}
									exclude={excludeIds}
									field={fieldName}
									limit={5}
									placeholder={`Find ${this.props.label} to add..`}
									onFind={entry => {
										if (!entry || this.state.value.some(e => e.id === entry.id)) return;
										const value = [...this.state.value, entry];
										this.runChange(value);
									}}
									onRender={result => this.renderResult(result)}
								/>
						)}
					</footer>

					{/* Modal for creating or editing attribute values */}
					{this.state.showCreateOrEditModal && (
						<Modal
							title={this.state.entityToEditId ? `Edit ${this.props.contentType}` : `Create New ${this.props.contentType}`}
							visible
							isExtraLarge
							onClose={() => this.setState({ showCreateOrEditModal: false, entityToEditId: null })}
						>
							<AutoForm
								canvasContext={this.props.canvasContext || this.props.currentContent}
								modalCancelCallback={() => this.setState({ showCreateOrEditModal: false, entityToEditId: null })}
								endpoint={this.props.contentType}
								singular={this.props.contentType}
								plural={this.props.contentType + "s"}
								id={this.state.entityToEditId || null}
								onActionComplete={entity => {
									const value = [...this.state.value];
									const index = value.findIndex(v => v.id === entity.id);
									if (index !== -1) value[index] = entity;
									else value.push(entity);

									this.setState({
										showCreateOrEditModal: false,
										entityToEditId: null
									});

									this.runChange(value);
								}}
							/>
						</Modal>
					)}

					{this.state.specifyAttributeValue?.attribute && (
						<SpecifyModal
							{...this.state.specifyAttributeValue}
							onChange={(entry) => {
								if (!entry || this.state.value.some(e => e.id === entry.id)) return;
								const value = [...this.state.value, entry];
								this.setState({ specifyAttributeValue: null });
								this.runChange(value);
							}}
							closeAction={() => {
								this.setState({ specifyAttributeValue: null });
							}}
						/>
					)}
				</div>
			</>
		);
	}

	/**
	 * Render a required attribute in a list entry.
	 *
	 * This method generates a <li> element representing a required product attribute.
	 * It displays the attribute name, its current value if present, or a UI allowing the user
	 * to specify a value. Also includes a remove button when a value exists.
	 *
	 * @param {ProductAttribute} attribute - The attribute to render (name, units, etc.).
	 * @param {number} idx - The index of this entry in the list (used as React key).
	 * @param {ProductAttributeValue | undefined} attributeValue - The currently assigned value, if any.
	 *
	 * @returns {JSX.Element} A React list item representing the attribute and its value/controls.
	 */
	renderRequiredAttribute(attribute, idx, attributeValue) {

		// Determine which field of the attributeValue to display (usually 'value', but can be overridden)
		let displayFieldName = this.getDisplayFieldName();

		return (
			<li key={idx} className="admin-multiselect__entry">

				{/* Display the attribute name */}
				<div>
					{attribute ? attribute.name : ''}
				</div>

				{/* If a value exists for this attribute, display it */}
				{attributeValue?.[displayFieldName] ? (
					<div>
						{/*
                        If the display field name contains "Json", we render it using a <Canvas> component.
                        Otherwise, we display the raw value directly.
                    */}
						{displayFieldName.indexOf("Json") !== -1 ? (
							<Canvas>{attributeValue?.[displayFieldName]}</Canvas>
						) : (
							attributeValue?.[displayFieldName]
						)}
						{/* Append the attribute's units, if available */}
						{attribute ? attribute.units : ''}
					</div>
				) : (
					// If no value exists, show a placeholder UI for specifying the value
					// Currently left empty (could be replaced with a button or input)
					<></>
				)}

				{/* Placeholder for additional entry options such as media previews */}
				<div className="admin-multiselect__entry-options">
					{/*
						Example code commented out for media previews (images/videos):
						Checks if a media reference field exists and displays it as an image or video icon.
					*/}
					{/*{mediaRefFieldName && mediaRefFieldName.length > 0 && entry[mediaRefFieldName] && entry[mediaRefFieldName].length > 0 &&*/} 
					{/* <div className="admin-multiselect__avatar">*/} 
					{/* {fileRef.isImage(entry[mediaRefFieldName]) && <>*/} 
					{/* <Image fileRef={entry[mediaRefFieldName]} size={32} />*/} 
					{/* </>}*/} 
					{/* {fileRef.isVideo(entry[mediaRefFieldName]) && <>*/} 
					{/* <i className="fa fa-2x far-file"></i>*/} 
					{/* </>}*/} 
					{/* </div>*/} 
					{/*}*/}
				</div>

				{/* Render either a Remove button or a Specify button based on whether a value exists */}
				{attributeValue?.[displayFieldName] ? (
					// Remove button: deletes the current attribute value
					<button
						className="btn btn-sm btn-outline-danger btn-entry-select-action btn-remove-entry"
						title={`Remove`}
						onClick={() => this.remove(attributeValue)}
					>
						<i className="fal fa-fw fa-times"></i> <span className="sr-only">{`Remove`}</span>
					</button>
				) : (
					// Specify button: triggers smart value creation in the parent component
					<Button
						onClick={() =>
							this.setState({
								specifyAttributeValue: {
									attribute, 
									value: attributeValue,
									displayFieldName
								}
							})
						}
					>
						{`Specify`}
					</Button>
				)}
			</li>
		);
	}

}
/**
 * @typedef {Object} SpecifyModalProps
 * @property {string} displayFieldName - The key name used to display each attribute value (e.g., "name" or "label").
 * @property {Object} attribute - The attribute metadata for which the value is being specified.
 * @property {number} attribute.id - The unique identifier of the attribute.
 * @property {string} attribute.name - The name of the attribute being specified.
 * @property {string} [attribute.units] - Optional units for the attribute value (e.g., "kg", "cm").
 * @property {any} [value] - The currently selected or initial value (if any).
 * @property {(value: Object) => void} onChange - Callback invoked when a value is selected or created.
 * @property {() => void} closeAction - Callback invoked when the modal should close.
 */

/**
 * Renders a modal that allows the user to either:
 * - Select an existing attribute value, or
 * - Create a new one (with optional units).
 *
 * This component supports:
 * - Live filtering of existing values.
 * - Inline creation of new attribute values using an API.
 * - Handling both selection and creation with validation.
 *
 * @param {SpecifyModalProps} props - Component properties.
 * @returns {JSX.Element} A modal interface for specifying attribute values.
 * @constructor
 */
const SpecifyModal = (props) => {
	const { displayFieldName, attribute, value, onChange, closeAction } = props;

	/** @type {[string|null, React.Dispatch<React.SetStateAction<string|null>>]} */
	const [newValue, setNewValue] = useState(null);

	/** @type {[string|null, React.Dispatch<React.SetStateAction<string|null>>]} */
	const [error, setError] = useState(null);

	/**
	 * Handles selecting an existing value.
	 * Appends the attribute metadata to the value object,
	 * triggers the `onChange` callback, and closes the modal.
	 *
	 * @param {Object} value - The selected attribute value.
	 */
	const chooseValue = (value) => {
		value.attribute = attribute;
		onChange(value);
		closeAction();
	};

	/**
	 * Creates a new attribute value using the API.
	 * Returns a promise that resolves with the created value
	 * or rejects with an error message.
	 *
	 * @returns {Promise<Object>} A promise resolving to the created attribute value.
	 */
	const createValue = () => {
		return new Promise((resolve, reject) => {
			if (!newValue) {
				reject('Value is empty');
				return;
			}
			
			productAttributeValueApi.list({
				query: "Value = ?",
				args: [newValue]
			})
			.then((result) => {
				if (result.results.length != 0) {
					resolve(result.results[0]);
				} else {
					productAttributeValueApi.create({
						productAttributeId: attribute.id,
						value: newValue
					})
						.then(resolve)
						.catch(reject);
				}
			})
		});
	};

	return (
		<Modal
			title={`Specify value for ${attribute.name}`}
			visible
			isExtraLarge
			onClose={() => closeAction?.()}
			className={'specify-modal'}
		>
			<div className={'create-new-attr-value'}>
				{error && <Alert variant={'danger'}>{error}</Alert>}
				<Input
					type={'text'}
					label={`Specify new value ${attribute.units ? `(in ${attribute.units})` : ''}`}
					onInput={(e) => setNewValue(e.target.value)}
					onKeyDown={(e) => {
						if (e.key === 'Enter') {
							createValue()
								.then(value => {
									chooseValue(value);
									setNewValue(null);
								})
								.catch(err => setError(err));
						}
					}}
				/>
				<Button 
					onClick={() => {
						createValue()
							.then(value => {
								chooseValue(value);
								setNewValue(null);
							})
							.catch(err => setError(err));
					}} 
					disabled={!Boolean(newValue)}
				>{`Create`}</Button>
			</div>

			{` - Or - `}

			<div className={'existing-values'}>
				<div className={'mb-3'}>
					<label>{`Existing value?`}</label>
				</div>

				<Loop
					over={productAttributeValueApi}
					paged={{ pageSize: 10 }}
					filter={{
						query: 'ProductAttributeId = ? and Value startsWith ?',
						args: [attribute.id, newValue]
					}}
					orNone={() => (
						<Alert variant={'warning'}>{`No existing values, create one above`}</Alert>
					)}
				>
					{(value) => (
						<div
							onClick={() => chooseValue(value)}
							className={'existing-value'}
						>
                            <span>
                                {value[displayFieldName]}{attribute.units}
                            </span>
							<Button
								type={'button'}
								onClick={() => chooseValue(value)}
							>
								{`Choose`}
							</Button>
						</div>
					)}
				</Loop>
			</div>
		</Modal>
	);
};
