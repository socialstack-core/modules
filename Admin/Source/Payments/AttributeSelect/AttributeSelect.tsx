// =========================
// React Imports
// =========================
import React, { useState, useEffect, useRef } from "react";

// ===========================
// UI Imports
// ===========================
import Search from 'UI/Search';
import Modal from 'UI/Modal';
import Canvas from 'UI/Canvas';
import Image from 'UI/Image';
import Link from 'UI/Link';
import * as fileRef from 'UI/FileRef';
import Button from "UI/Button";
import Input from "UI/Input";
import Loop from "UI/Loop";
import Alert from "UI/Alert";

// ===========================
// API Imports
// =========================== 
import productAttributeValueApi, { ProductAttributeValue } from 'Api/ProductAttributeValue';
import { ProductAttribute } from 'Api/ProductAttribute';

// ===========================
// Prop Types
// ===========================
export interface AttributeSelectProps {
	value?: (ProductAttributeValue | number)[];
	defaultValue?: (ProductAttributeValue | number)[];
	requiredAttributes?: ProductAttribute[];
	max?: number;
	label?: string;
	hideLabel?: boolean;
	name?: string;
	field?: string;
	displayField?: string;
	onRawChange?: (e: { target: { value: number[] }; fullValue: ProductAttributeValue[] }) => void;
	onChange?: (e: { target: { value: number[] }; fullValue: ProductAttributeValue[] }) => void;
	canvasContext?: any;
	currentContent?: any;
	contentType?: string;
}

// ===========================
// Caching
// ===========================
/**
 * A reference to the AutoForm component.
 */	
let AutoForm: any = null;

// ===========================
// Component Export
// ===========================
/**
 * A general use "multi-selection"; primarily used for tags and categories.
 */
const AttributeSelect: React.FC<AttributeSelectProps> = (props) => {
	
	const fieldRef = useRef<HTMLDivElement | null>(null);
	const inputRef = useRef<HTMLInputElement | null>(null);

	// Parse initial values
	const getInitialValueAndLoadState = () => {
		const rawInitVal = (props.value || props.defaultValue || []).filter((t: any) => t != null);
		let mustLoad = false;
		let parsedVal: ProductAttributeValue[] = [];

		if (rawInitVal.length) {
			parsedVal = rawInitVal.map((value: any, idx: number) => {
				if (typeof value === 'number') {
					if (Number.isInteger(value)) {
						mustLoad = true;
						return {
							id: value
						} as ProductAttributeValue;
					}
				}
				
				if (value && typeof value === 'object' && value.id) {
					return value as ProductAttributeValue;
				}

				console.warn('[AttributeSelect][props] - Expected initVal.(value|defaultValue) index ' + idx + ' to be an integer. You supplied "' + value + '"', value);
				return null;
			}).filter((t): t is ProductAttributeValue => t != null);
		}

		return { parsedVal, mustLoad };
	};

	const { parsedVal, mustLoad: initialMustLoad } = getInitialValueAndLoadState();

	const [value, setValue] = useState<ProductAttributeValue[]>(parsedVal);
	const [mustLoad, setMustLoad] = useState<boolean>(initialMustLoad);
	const [showCreateOrEditModal, setShowCreateOrEditModal] = useState<boolean>(false);
	const [entityToEditId, setEntityToEditId] = useState<number | null>(null);
	const [specifyAttributeValue, setSpecifyAttributeValue] = useState<any | null>(null);
	const [error, setError] = useState<string | null>(null);

	// Load dynamic AutoForm if not already cached
	if (!AutoForm) {
		AutoForm = require("Admin/AutoForm").default;
	}

	// Fetch detailed attributes on mount (replaces componentDidMount)
	useEffect(() => {
		if (value.length > 0) {
			const filter = {
				query: "Id=[?]",
				args: [value.map(e => e.id)]
			};
			
			productAttributeValueApi.list(filter, [productAttributeValueApi.includes.attribute]).then((response: any) => {
				// Loading the values and preserving order:
				const idLookup: Record<string, ProductAttributeValue> = {};
				response.results.forEach((r: ProductAttributeValue) => {
					idLookup[r.id + ''] = r;
				});

				setMustLoad(false);
				setValue((currentVal) => currentVal.map(e => idLookup[e.id + '']).filter((t): t is ProductAttributeValue => t != null));
			});
		} else {
			setMustLoad(false);
		}
	}, []);

	// Sync value from props (replaces componentWillReceiveProps)
	useEffect(() => {
		if (props.value) {
			setValue(props.value.filter((t): t is ProductAttributeValue => t != null));
		}
	}, [props.value]);

	// Keep mutable references of state values for dynamic form validation
	const valueRef = useRef<ProductAttributeValue[]>(value);
	valueRef.current = value;

	const requiredAttributesRef = useRef<ProductAttribute[]>([]);
	requiredAttributesRef.current = Array.isArray(props.requiredAttributes) ? props.requiredAttributes : [];

	const throwIfMissingValue = () => {
		const missingValues: ProductAttribute[] = [];
		const currentRequired = requiredAttributesRef.current;
		const currentValue = valueRef.current;
		
		currentRequired.forEach((attribute) => {
			if (!currentValue.find(val => val.productAttributeId === attribute.id)) {
				missingValues.push(attribute);
			}	
		});
		
		if (missingValues.length !== 0) {
			// focus the element. 
			fieldRef.current?.scrollIntoView({
				behavior: "smooth",
			});
			
			const errorMsg = `Missing ${missingValues.length} required attributes: ${missingValues.map(attr => attr.name).join(', ')}`;
			setError(errorMsg);
			
			throw {
				type: 'validation',
				message: errorMsg
			};
		}
	};

	const getRequiredAttributes = () => {
		return requiredAttributesRef.current;
	};
	
	const getFieldName = () => {
		return props.field || 'value';
	};
	
	const getDisplayFieldName = () => {
		let displayFieldName = props.displayField || getFieldName();
		if (displayFieldName.length) {
			displayFieldName = displayFieldName[0].toLowerCase() + displayFieldName.substring(1);
		}
		return displayFieldName;
	};

	const runChange = (newValue: ProductAttributeValue[]) => {
		setValue(newValue);
		const e = { target: { value: newValue.map(item => item.id) }, fullValue: newValue };
		props.onRawChange && props.onRawChange(e);
		props.onChange && props.onChange(e);
	};

	const remove = (entry: ProductAttributeValue) => {
		const newValue = value.filter(t => t !== entry && t != null);
		runChange(newValue);
	};

	const renderResult = (result: any) => {
		if (!result.attribute) {
			console.error('Orphaned attribute', result);
			return null;
		}
		return (
			<>
				{result.attribute.name}{`:`}&nbsp;{result.value}{result.attribute.units ? result.attribute.units : ''}
			</>
		);
	};

	const renderRequiredAttribute = (attribute: ProductAttribute, idx: number, attributeValue: ProductAttributeValue | undefined) => {
		const displayFieldName = getDisplayFieldName();

		return (
			<li key={idx} className="admin-multiselect__entry">
				{/* Display the attribute name */}
				<div>
					{attribute ? attribute.name : ''}
				</div>

				{/* If a value exists for this attribute, display it */}
				{attributeValue && (attributeValue as any)[displayFieldName] ? (
					<div>
						{displayFieldName.indexOf("Json") !== -1 ? (
							<Canvas>{(attributeValue as any)[displayFieldName]}</Canvas>
						) : (
							(attributeValue as any)[displayFieldName]
						)}
						{/* Append the attribute's units, if available */}
						{attribute ? attribute.units : ''}
					</div>
				) : (
					<></>
				)}

				{/* Placeholder for additional entry options such as media previews */}
				<div className="admin-multiselect__entry-options">
				</div>

				{/* Render either a Remove button or a Specify button based on whether a value exists */}
				{attributeValue && (attributeValue as any)[displayFieldName] ? (
					<Button sm outlined variant="danger" className="btn-entry-select-action btn-remove-entry" title={`Remove`} onClick={() => remove(attributeValue)}>
						<i className="fal fa-fw fa-times"></i> <span className="sr-only">{`Remove`}</span>
					</Button>
				) : (
					<Button
						onClick={() =>
							setSpecifyAttributeValue({
								attribute, 
								value: attributeValue,
								displayFieldName
							})
						}
					>
						{`Specify`}
					</Button>
				)}
			</li>
		);
	};

	const displayFieldName = getDisplayFieldName();
	const mediaRefFieldName = 'featureRef';
	const excludeIds = value.map(a => a.id);

	let atMax = false;
	if (props.max && props.max > 0) {
		atMax = (value.length >= props.max);
	}

	return (
		<>
			<div className="admin-multiselect mb-3" ref={fieldRef}>
				{props.label && !props.hideLabel && (
					<label className="form-label">
						{props.label}{' '}
						<Link href='/en-admin/attribute/'><i className="fa fa-external-link" /></Link>
					</label>
				)}
				
				{error && (
					<>
						<br />
						<span className={'validation-error'}>{error}</span>
					</>
				)}

				<ul className="admin-multiselect__entries">
					{getRequiredAttributes().length ? (
						getRequiredAttributes().map((attribute, idx) => {
							const existingValue = value.find(
								entry => entry?.attribute?.id === attribute.id
							);
							return renderRequiredAttribute(attribute, idx, existingValue);
						})
					) : null}

					{value
						.filter((entry) => {
							if (!getRequiredAttributes().length) return true;
							return !getRequiredAttributes().find(attr => attr.id === entry.productAttributeId);
						})
						.map((entry, i) => (
							<li key={i + (entry.id || 0)} className="admin-multiselect__entry">
								<div>
									{entry.attribute ? entry.attribute.name : ''}
								</div>

								<div>
									{displayFieldName.indexOf("Json") !== -1
										? <Canvas>{(entry as any)[displayFieldName]}</Canvas>
										: (entry as any)[displayFieldName]
									}
									{entry.attribute ? entry.attribute.units : ''}
								</div>

								<div className="admin-multiselect__entry-options">
									{mediaRefFieldName && (entry as any)[mediaRefFieldName] && ((entry as any)[mediaRefFieldName]).length > 0 &&
										<div className="admin-multiselect__avatar">
											{fileRef.isImage((entry as any)[mediaRefFieldName]) && (
												<Image fileRef={(entry as any)[mediaRefFieldName]} size={32} />
											)}
											{fileRef.isVideo((entry as any)[mediaRefFieldName]) && (
												<i className="fa fa-2x far-file"></i>
											)}
										</div>
									}

									<Button sm outlined variant="danger" className="btn-entry-select-action btn-remove-entry"
										title={`Remove`}
										onClick={() => remove(entry)}
									>
										<i className="fal fa-fw fa-times"></i>
										<span className="sr-only">{`Remove`}</span>
									</Button>
								</div>
							</li>
						))
					}
				</ul>

				<input
					type="hidden"
					name={props.name}
					ref={ele => {
						inputRef.current = ele;
						if (ele != null) {
							// @ts-ignore custom SocialStack dynamic evaluation
							ele.onGetValue = (v: any, input: any, e: any) => {
								if (input !== inputRef.current) {
									return v;
								}
								throwIfMissingValue();
								return valueRef.current.map(entry => entry.id);
							};
						}
					}}
				/>

				<footer className="admin-multiselect__footer">
					{atMax ? (
						<span className="admin-multiselect__search-max">
							<i>{`Max of ${props.max} added`}</i>
						</span>
					) : (
						<Search
							endpoint={productAttributeValueApi.list}
							includes={[productAttributeValueApi.includes.attribute]}
							exclude={excludeIds}
							field={getFieldName()}
							limit={5}
							placeholder={`Find ${props.label} to add..`}
							onFind={(entry: any) => {
								if (!entry || value.some(e => e.id === entry.id)) return;
								const newValue = [...value, entry];
								runChange(newValue);
							}}
							onRender={result => renderResult(result)}
						/>
					)}
				</footer>

				{showCreateOrEditModal && (
					<Modal
						title={entityToEditId ? `Edit ${props.contentType}` : `Create New ${props.contentType}`}
						visible
						isExtraLarge
						onClose={() => {
							setShowCreateOrEditModal(false);
							setEntityToEditId(null);
						}}
					>
						<AutoForm
							canvasContext={props.canvasContext || props.currentContent}
							modalCancelCallback={() => {
								setShowCreateOrEditModal(false);
								setEntityToEditId(null);
							}}
							endpoint={props.contentType}
							singular={props.contentType}
							plural={props.contentType + "s"}
							id={entityToEditId}
							onActionComplete={(entity: any) => {
								const newValue = [...value];
								const index = newValue.findIndex(v => v.id === entity.id);
								if (index !== -1) newValue[index] = entity;
								else newValue.push(entity);

								setShowCreateOrEditModal(false);
								setEntityToEditId(null);
								runChange(newValue);
							}}
						/>
					</Modal>
				)}

				{specifyAttributeValue?.attribute && (
					<SpecifyModal
						{...specifyAttributeValue}
						onChange={(entry: any) => {
							if (!entry || value.some(e => e.id === entry.id)) return;
							const newValue = [...value, entry];
							setSpecifyAttributeValue(null);
							runChange(newValue);
						}}
						closeAction={() => {
							setSpecifyAttributeValue(null);
						}}
					/>
				)}
			</div>
		</>
	);
};

// ===========================
// SpecifyModal Definition
// ===========================
interface SpecifyModalProps {
	displayFieldName: string;
	attribute: ProductAttribute;
	value?: ProductAttributeValue;
	onChange: (value: ProductAttributeValue) => void;
	closeAction: () => void;
}

const SpecifyModal: React.FC<SpecifyModalProps> = (props) => {
	const { displayFieldName, attribute, value, onChange, closeAction } = props;

	const [newValue, setNewValue] = useState<string | null>(null);
	const [error, setError] = useState<any>(null);

	const chooseValue = (val: ProductAttributeValue) => {
		val.attribute = attribute;
		onChange(val);
		closeAction();
	};

	const createValue = (): Promise<ProductAttributeValue> => {
		return new Promise((resolve, reject) => {
			if (!newValue) {
				reject('Value is empty');
				return;
			}
			
			productAttributeValueApi.list({
				query: "Value = ?",
				args: [newValue]
			})
			.then((result: any) => {
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
			});
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
					onInput={(e: any) => setNewValue(e.target.value)}
					onKeyDown={(e: any) => {
						if (e.key === 'Enter') {
							createValue()
								.then(val => {
									chooseValue(val);
									setNewValue(null);
								})
								.catch(err => setError(err));
						}
					}}
				/>
				<Button 
					onClick={() => {
						createValue()
							.then(val => {
								chooseValue(val);
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
					{(val: ProductAttributeValue) => (
						<div
							onClick={() => chooseValue(val)}
							className={'existing-value'}
						>
							<span>
								{(val as any)[displayFieldName]}{attribute.units}
							</span>
							<Button
								type={'button'}
								onClick={() => chooseValue(val)}
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

export default AttributeSelect;
