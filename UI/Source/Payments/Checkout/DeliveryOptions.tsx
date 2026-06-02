import { useEffect, useMemo, useState } from "react";
import { formatCurrency } from "UI/Functions/CurrencyTools";
import { ApiList } from "UI/Functions/WebRequest";
import Input from "UI/Input";
import { ShoppingCart } from 'Api/ShoppingCart';
import { Locale } from 'Api/Locale';
import { DeliveryOption } from 'Api/DeliveryOption';
import getConfig from 'UI/Config';


/**
 * Props for the DeliveryOptions component.
 */
interface DeliveryOptionsProps {
	shoppingCart?: ShoppingCart | undefined,
	locale?: Locale | undefined,
	activeDeliveryDay?: Date | undefined,
	setActiveDeliveryDay?: React.Dispatch<React.SetStateAction<Date | undefined>>,
	deliveryOption?: DeliveryOption | undefined,
	setDeliveryOption?: React.Dispatch<React.SetStateAction<DeliveryOption | undefined>>,
	estimates?: ApiList<DeliveryOption> | undefined,
	deliveryInformation?: string | undefined,
	setDeliveryInformation?: React.Dispatch<React.SetStateAction<string | undefined>>
}


interface DeliveryInformation {
	price: uint,
	taxApportionment: number,
	priceLesstTax: uint,
	currency: string,
	deliveryCode: string,
	requestedDeliveryDate: string,
	forFreeDelivery: uint,
	isFreeDelivery: boolean
}

interface DeliveryDayConfig {
	standardCourierCode: string,
	saturdayCourierCode: string
}

/**
 * The DeliveryOptions React component.
 * @param props React props.
 */
const DeliveryOptions: React.FC<DeliveryOptionsProps> = (props) => {
	let {shoppingCart, locale, estimates} = props;
	const deliveryConfig = getConfig<DeliveryDayConfig>("DeliveryDay")?.[0];
	const currencyCode = locale?.currencyCode

	const [localActiveDeliveryDay, setLocalActiveDeliveryDay] = useState<Date | undefined>();
	const [localDeliveryOption, setLocalDeliveryOption] = useState<DeliveryOption | undefined>();
	const activeDeliveryDay = props.activeDeliveryDay ?? localActiveDeliveryDay;
	const setActiveDeliveryDay = props.setActiveDeliveryDay ?? setLocalActiveDeliveryDay;
	const deliveryOption = props.deliveryOption ?? localDeliveryOption;
	const setDeliveryOption = props.setDeliveryOption ?? setLocalDeliveryOption;
	const [deliveryOptionMap, setDeliveryOptionMap] = useState<Map<DeliveryOption, DeliveryInformation> | undefined>();
	const [selectedDeliveryOptionInfo, setSelectedDeliveryOptionInfo] = useState<DeliveryInformation | undefined>();


	const getDeliveryFromMap = (code?: string) => {
		let result: DeliveryOption | undefined

		deliveryOptionMap?.forEach((value, key) => {
			if (!result && value.deliveryCode === code) {
				result = key;
			}
		});

		return result;
	};

	//Get individual delivery options
	const standardCourierDelivery = useMemo<DeliveryOption | undefined>(() => getDeliveryFromMap(deliveryConfig?.standardCourierCode),[deliveryOptionMap]);
	const saturdayCourierDelivery = useMemo<DeliveryOption | undefined>(() => getDeliveryFromMap(deliveryConfig?.saturdayCourierCode),[deliveryOptionMap]);

	//Extract costs from delivery options
	const standardCourierCost = standardCourierDelivery ? deliveryOptionMap?.get(standardCourierDelivery)!.price : undefined;
	const formattedstandardCourierCost = standardCourierCost ? formatCurrency(standardCourierCost, {currencyCode}) : '';
	
    const saturdayCourierCost = saturdayCourierDelivery ? deliveryOptionMap?.get(saturdayCourierDelivery)!.price : undefined;
	const formattedSaturdayCourierCost = saturdayCourierCost ? formatCurrency(saturdayCourierCost, { currencyCode }) : '';

	useEffect(() => {
		if(!estimates){
			return;
		}

		//Setup our delivery options map
		var newMap = new Map<DeliveryOption, DeliveryInformation>();
		estimates.results.forEach(x => {
			newMap.set(x, JSON.parse(x.informationJson!))
		});
		setDeliveryOptionMap(newMap);

	}, [estimates]);

	useEffect(() => {
		const info = deliveryOption ? deliveryOptionMap?.get(deliveryOption) : undefined;
		setSelectedDeliveryOptionInfo(info);
	}, [deliveryOption, deliveryOptionMap]);

	useEffect(() => {
		if(!selectedDeliveryOptionInfo){
			return;
		}

		//Update the active delivery date
		setActiveDeliveryDay(new Date(selectedDeliveryOptionInfo.requestedDeliveryDate));
	},[selectedDeliveryOptionInfo])

	const formatDate = (date: Date) => {
		console.log(date)
		return date.toLocaleDateString("en-GB", {
			day: "numeric",
			month: "long"
		});
	}

	const sdoPrice = selectedDeliveryOptionInfo?.price;
	const formattedDeliveryCost = sdoPrice ? formatCurrency(sdoPrice, { currencyCode } ) : '';
	const formattedCurrentDeliveryDate = (activeDeliveryDay ? formatDate(activeDeliveryDay) : "");
	
	const deliverySubtitle = `${formattedCurrentDeliveryDate}  |  ${formattedDeliveryCost}`;

	return <div className="payment-checkout__delivery">
		<h4>{deliverySubtitle}</h4>
		
		<div className="payment-checkout__delivery-internal">
			{(standardCourierDelivery || saturdayCourierDelivery) && 
				<div className={`payment-checkout__delivery-tile "payment-checkout__delivery-tile--premium"`}>

					{standardCourierDelivery && <Input type="radio" sm noWrapper name="delivery-options" checked={selectedDeliveryOptionInfo?.deliveryCode === deliveryConfig?.standardCourierCode}
						onClick={() => setDeliveryOption(standardCourierDelivery)} 
						label={<>
							<span className="payment-checkout__delivery-option">
								<span>{`Standard Courier - Next Day Delivery`}</span>
								<span>{formattedstandardCourierCost}</span>
							</span>
						</>} />}

					{saturdayCourierDelivery && <Input type="radio" sm noWrapper name="delivery-options" checked={selectedDeliveryOptionInfo?.deliveryCode === deliveryConfig?.saturdayCourierCode}
						onClick={() => setDeliveryOption(saturdayCourierDelivery)} 
						label={<>
							<span className="payment-checkout__delivery-option">
								<span>{`Saturday Delivery`}</span>
								<span>{formattedSaturdayCourierCost}</span>
							</span>
						</>} />}
				</div>
			}				
		</div>

		{props.setDeliveryInformation && 
			<Input type="text" 
			label={`Delivery Instructions`}
			defaultValue={props.deliveryInformation}
			onChange={e => {
				const input = (e.target as HTMLInputElement);
				props.setDeliveryInformation!(input.value);
			}}
		/>	
		}
	</div>;
}

export default DeliveryOptions;