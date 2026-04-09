import { DefaultInputType } from "UI/Input/Default";
import { useSession } from "UI/Session";
import Input from "UI/Input";
import { formatCurrency } from "UI/Functions/CurrencyTools";
import { useEffect, useState } from "react";
import localeApi, { Locale } from "Api/Locale";
import Loading from "UI/Loading";

/**
 * Safely coerces any value into a number (integer).
 * Strings, undefined, null → always a valid number.
 */
const toInt = (v: unknown): number => {
	const n = parseInt(String(v), 10);
	return Number.isFinite(n) ? n : 0;
};

/**
 * Extracts a currency symbol using Intl.NumberFormat.
 * This guarantees correct symbols worldwide.
 */
const getCurrencySymbol = (currencyCode: string, locale: string): string => {
	try {
		const parts = new Intl.NumberFormat(locale, {
			style: "currency",
			currency: currencyCode,
		}).formatToParts(0);

		const symbol = parts.find(p => p.type === "currency")?.value;
		return symbol ?? currencyCode;
	} catch {
		// Fallback
		return currencyCode;
	}
};

const Price: React.FC<CustomInputTypeProps<"price">> = props => {

	const [price, setPrice] = useState<number | null>(null);

	const [locale, setLocale] = useState<Locale>();

	useEffect(() => {
		if (!locale) {
			if (props?.field?.locale) {
				setLocale(props.field.locale);
				return;
			}
			// otherwise load the default.
			localeApi.load(1 as uint).then(setLocale);
		}
	}, [locale]);

	/**
	 * Initialise the price value only once.
	 * - Prevents looping.
	 * - Handles missing, null or empty data safely.
	 */
	const [initialised, setInitialised] = useState(false);
	useEffect(() => {
		if (!initialised) {
			const fallback =
				props?.field?.defaultValue ??
				props?.field?.value;

			if (fallback !== undefined && fallback !== null && fallback !== "") {
				setPrice(toInt(fallback));
			}
			setInitialised(true);
		}
	}, [initialised, props?.field]);

	const currencyCode = locale?.currencyCode;
	const localeCode = locale?.code;

	if (!currencyCode || !localeCode) {
		return (
			<Loading />
		)
	}
	
	if (!initialised) return null;

	const currencySymbol = getCurrencySymbol(currencyCode, localeCode);

	return (
		<div className="price-editor">

			{/* Decorative currency symbol (auto-detected via Intl) */}
			<div className="currency-symbol">
				{currencySymbol}
			</div>
			<div className={'price-field'}>
				<Input
					{...props?.field}
					defaultValue={price !== null ? formatCurrency(price, {
						currencyCode,
						hideSymbol: true,
					}) : ""}
					name={undefined}
					label={undefined}
					help={undefined}
					type="number"
					step={0.01}
					onChange={(e) => {
						const rawValue = (e.target as HTMLInputElement).value;
						if (rawValue.trim() === '') {
							setPrice(null);
						} else {
							setPrice(
								Math.ceil(
									parseFloat(rawValue) * 100
								)
							);
						}
					}}
				/>
				<input
					type={'hidden'}
					name={props.field.name}
					value={(price !== null && !isNaN(price)) ? price : ""}
				/>
			</div>
		</div>
	);
};

window.inputTypes["price"] = Price;

declare global {
	interface InputPropsRegistry {
		"price": DefaultInputType & CustomInputTypeProps<"number"> & {
			locale?: Locale
		};
	}
}
