import { useEffect } from "react";
import Button from 'UI/Button';
import { PaymentMethod } from 'Api/PaymentMethod';

/**
 */
interface OpayoExternalProps
{
	formRef: React.RefObject<HTMLFormElement>;
	paymentMethod: PaymentMethod;
    disabled?: boolean;
}

/**
 * The OpayoExternal React component.
 * @param props React props.
 */
const OpayoExternal: React.FC<OpayoExternalProps> = (props) => {
	const { 
		formRef,
		paymentMethod,
        disabled
	} = props;

	var paymentGateways = globalThis.paymentGateways = globalThis.paymentGateways || {};

	useEffect(() => {
		if (!paymentMethod || paymentGateways.hostedPageEnabled) {
			return;
		}

		paymentGateways.onLoadForm && paymentGateways.onLoadForm(paymentMethod.sessionId).then(() => {
			// sometimes the iframe loads with 0 height
			const container = document.getElementById("sp-container");
			// Remove the inline height property
			container.style.removeProperty("height");
		});

	},[paymentMethod]);


	return <>
		{paymentGateways.hostedPageEnabled ?
	
			<Button disabled={disabled} type="submit" name='paymentMethod' variant='secondary' value={JSON.stringify(paymentMethod)}>
				<i className="fal fa-fw fa-credit-card" />
				<span>
					{`Pay by Card`}
				</span>
			</Button>
		:
			<form id="opayoPaymentForm" onSubmit={(event) => {
				const form = event.target as HTMLFormElement;
				form.submit = () => {
					const fields = form.elements;
					let identi: Element | null = null;

					for (let i = 0; i < fields.length; i++) {
						const field = fields[i];
						if (field?.name === 'card-identifier') {
							identi = field as Element;
							break;
						}
					}

					if (identi instanceof HTMLInputElement) {
						paymentMethod.gatewayToken = identi.value;

						const paymentMethodIdentifier = document.createElement('input');
						paymentMethodIdentifier.type = 'hidden';
						paymentMethodIdentifier.name = 'paymentMethod';
						paymentMethodIdentifier.value = JSON.stringify(paymentMethod);

						formRef?.current?.appendChild(paymentMethodIdentifier);
						formRef?.current?.requestSubmit();
					}
				};

				return false;
			}}>

				{/* The payment gateway will inject the card form into this container */}
				<div id="sp-container"></div>
			
				<Button disabled={disabled} type="submit">
					<i className="fal fa-fw fa-credit-card" />
					<span>
						{`Confirm Purchase`}
					</span>
				</Button>
			</form>
		}
	</>;
		
};

export default OpayoExternal;