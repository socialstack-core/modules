import { useEffect, useState, useRef } from "react";
import Modal from 'UI/Modal';
import Form from 'UI/Form';
import Button from 'UI/Button';
import { useSession } from 'UI/Session';
import { formatCurrency, formatPOA } from "UI/Functions/CurrencyTools";
import localeApi, { Locale } from 'Api/Locale';
import Input from 'UI/Input';
import Loading from 'UI/Loading';

interface Price {
	Amount: Record<string, uint>;
	MinimumQuantity: uint;
}

interface SelectedPrice {
	index: number;
	price: Price;
}

interface PriceTableProps {
	readonly?: boolean;
	hideLabel?: boolean;
	value?: string;
	defaultValue?: string;
	label?: string;
}

const PriceTable: React.FC = (props: PriceTableProps) => {
	const inputRef = useRef<HTMLInputElement | null>(null);
	const { session } = useSession();
	const { locale } = session;
	const [locales, setLocales] = useState<Locale[] | null>(null);

    const readonly = props.readonly || false;
	
	const [value, setValue] = useState<Price[] | undefined>(() => {
		var initValString = (props.value || props.defaultValue || '');
		console.log(initValString);
		var initValue = initValString ? JSON.parse(initValString) as Price[] : [] as Price[];
		return initValue;
	});
	const [showModal, setShowModal] = useState<boolean>(false);
	const [entityToEdit, setEntityToEdit] = useState<SelectedPrice | undefined>();

	useEffect(() => {
		localeApi.listAll().then(result => setLocales(result.results));
	},[])

	useEffect(() => {
		const current = inputRef.current;

		if (current) {
			// @ts-ignore
			current.onGetValue = (v:string, input:HTMLInputElement, e: any) => {
				return value;
			}
		}

		return () => {
			if (current) {
				// @ts-ignore
				delete current.onGetValue;
			}
		};
	}, [value]);

	const onSetValue = (prices: Price[]) => {
		prices = prices.sort((a, b)=> a.MinimumQuantity - b.MinimumQuantity);
		setValue(prices);
	}
	
	const onRemove = (price: Price) => {
		if (!value) {
			return;
		}
		var newValue = value.filter(v => v != price);
		onSetValue(newValue);
	};

	if (!locales) {
		// Loading
		return <Loading />;
	}

	const localeCode = locale?.code || 'en';

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
						{`Amount (${locale?.currencyCode || 'GBP'})`}
					</th>
				</tr>
			</thead>
			<tbody>
				{
					value?.map((price, index)=> {
						return <tr>
							<td>
								{price.MinimumQuantity}
							</td>
							<td>
								{`${formatCurrency(price.Amount[localeCode] || 0 as int, {currencyCode: locale?.currencyCode})}`}
							</td>
							{!readonly && 
								<>
									<td>
										{locales && <Button sm outlined className="btn-entry-select-action btn-view-entry" title={`Edit`}
											onClick={e => {
												e.preventDefault();
												setEntityToEdit({index: index, price: price});
												setShowModal(true);
											}}>
											<i className="fal fa-fw fa-edit"></i> <span>{`Edit`}</span>
											</Button>}
									</td>
									<td>
									<Button sm outlined variant="danger" className="btn-entry-select-action btn-remove-entry" title={`Remove`}
										onClick={e => {
											e.preventDefault();
											onRemove(price);
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
				<Button sm outlined className="btn-entry-select-action btn-new-entry"
					onClick={e => {
						e.preventDefault();
						setEntityToEdit(undefined);
						setShowModal(true);
					}}
				>
					<i className="fal fa-fw fa-plus"></i> {`New`}
				</Button>
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
					setEntityToEdit(undefined);
				}}
			>
				<Form
					action={(entity: any) => {
						let newPrice = {MinimumQuantity: entity.MinimumQuantity, Amount: {}} as Price;
						delete entity.MinimumQuantity;

						//Convert from string values to numbers
						for(let i = 0; i < locales.length; i++){
							var code = locales[i].code;

							if (!code) {
								continue;
							}

							newPrice.Amount[code] = parseInt(entity[code]) as int;
						}

						var newValue = value || [];

						if (entityToEdit) {
							newValue[entityToEdit.index] = newPrice
						} else {
							newValue.push(newPrice)
						}
						
						setShowModal(false);
						setEntityToEdit(undefined);
						onSetValue(newValue);
						return Promise.resolve();
					}} 
					submitLabel={entityToEdit ? `Update` : `Create`}
				>
					<Input type="number" min="0" step="1" name={`minimumQuantity`} defaultValue={entityToEdit ? entityToEdit.price.MinimumQuantity : 0} label={`Minimum Quantity`}/>
					{locales.map(region => {
						if (!region.code) {
							return null;
						}
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
