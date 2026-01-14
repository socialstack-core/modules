import { Price } from 'Api/Price';
import { useEffect, useState, useRef } from "react";
import Modal from 'UI/Modal';
import Form from 'UI/Form';
import { useSession } from 'UI/Session';
import { formatCurrency, formatPOA } from "UI/Functions/CurrencyTools";
import localeApi from 'Api/Locale';
import Input from 'UI/Input';

const PriceTable: React.FC = (props) => {
	const [inputHandle, setInputHandle] = useState(null);
	const inputRef = useRef(null);
	const { session } = useSession();
	const { locale } = session;
	const [locales, setLocales] = useState(null);

    const readonly = props.readonly || false;
	
	const [value, setValue] = useState(() => {
		var initValString = (props.value || props.defaultValue || '');
		var initValue = initValString ? JSON.parse(initValString) : [];
		return initValue;
	});
	const [showModal, setShowModal] = useState(false);
	const [entityToEdit, setEntityToEdit] = useState();

	useEffect(() => {
		localeApi.listAll().then(result => setLocales(result.results));
	},[])

	useEffect(() => {
		const current = inputRef.current;

		if (current) {
			current.onGetValue = (v:string, input:HTMLInputElement, e: any) => {
				return value;
			}
		}

		return () => {
			if (current) {
				delete current.onGetValue;
			}
		};
	}, [value]);

	const onSetValue = (prices: Price[]) => {
		prices = prices.sort((a, b)=> a.MinimumQuantity - b.MinimumQuantity);
		setValue(prices);
	}
	
	const onRemove = (price: Price) => {
		var newValue = value.filter(v => v != price);
		onSetValue(newValue);
	};
	
    return <div className="price-tiers">

		{props.label && !props.hideLabel && (
			<label className="form-label">
				{props.label}
			</label>
		)}
		
		<table className="table">
			<thead>
				<tr>
					<th>
						{`Minimum Quantity`}
					</th>
					<th>
						{`Amount (${locale.currencyCode})`}
					</th>
				</tr>
			</thead>
			<tbody>
				{
					value.map((price, index)=> {
						return <tr>
							<td>
								{price.MinimumQuantity}
							</td>
							<td>
								{`${formatCurrency(price.Amount[locale.code], locale)}`}
							</td>
							{!readonly && 
								<>
									<td>
										{locales && <button className="btn btn-sm btn-outline-primary btn-entry-select-action btn-view-entry" title={`Edit`}
											onClick={e => {
												e.preventDefault();
												setEntityToEdit({index: index, price: price});
												setShowModal(true);
											}}>
											<i className="fal fa-fw fa-edit"></i> <span>{`Edit`}</span>
											</button>}
									</td>
									<td>
									<button className="btn btn-sm btn-outline-danger btn-entry-select-action btn-remove-entry" title={`Remove`}
										onClick={e => {
											e.preventDefault();
											onRemove(price);
										}}>
											<i className="fal fa-fw fa-times"></i> <span>{`Remove`}</span>
										</button>
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
				<button type="button" className="btn btn-sm btn-outline-primary btn-entry-select-action btn-new-entry"
					onClick={e => {
						e.preventDefault();
						setEntityToEdit(null);
						setShowModal(true);
					}}
				>
					<i className="fal fa-fw fa-plus"></i> {`New`}
				</button>
			}
		</footer>
		<input type="hidden" name={`priceTiers`} ref={inputRef} />
		{showModal &&
			<Modal
				title={entityToEdit ? `Edit Price Tier` : `Create New Price Tier`}
				visible
				isExtraLarge
				onClose={() => {
					setShowModal(false);
					setEntityToEdit(null);
				}}
			>
				<Form
					onSubmitted={entity => {
						let newPrice = {MinimumQuantity: entity.MinimumQuantity, Amount: {}};
						delete entity.MinimumQuantity;

						//Convert from string values to numbers
						for(let i = 0; i < locales.length; i++){
							var code = locales[i].code;
							newPrice.Amount[code] = Number(entity[code]);
						}

						var newValue = value;

						if (entityToEdit) {
							newValue[entityToEdit.index] = newPrice
						} else {
							newValue.push(newPrice)
						}
						
						setShowModal(false);
						setEntityToEdit(null);
						onSetValue(newValue);
					}} 
					submitLabel={entityToEdit ? `Update` : `Create`}
				>
					<Input type="number" min="0" step="1" name={`MinimumQuantity`} defaultValue={entityToEdit ? entityToEdit.price.MinimumQuantity : 0} label={`Minimum Quantity`}/>
					{locales.map(region => {
						return (<>
							<Input type="number" min="0" step="1" name={`${region.code}`} defaultValue={entityToEdit ? entityToEdit.price.Amount[region.code] : 0} label={`Amount for ${region.code}`}/>
						</>)
					})}
				</Form>
			</Modal>
		}
    </div>;
};

export default PriceTable;
