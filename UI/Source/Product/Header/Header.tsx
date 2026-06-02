import { Product } from 'Api/Product';
import { Locale } from 'Api/Locale';
import ProductStock from 'UI/Product/Stock';
import { useSession } from "UI/Session";
import { useCart } from 'UI/Payments/CartSession';
import { formatCurrency } from "UI/Functions/CurrencyTools";
import { getPriceTiers, getSellUnit } from "UI/Product/Functions";
import {useState} from "react";
import Button from "UI/Button";

interface HeaderProps {
	product: Product,
	currentVariant?: Product
}

/**
 * The Product Header React component.
 * @param props React props.
 */
const Header: React.FC<HeaderProps> = (props) => {
	const { product, currentVariant } = props;
	const isFeatured = product.isFeatured;
	const { lessTax } = useCart();
	const { session } = useSession();
	const { role, locale } = session;

	return (
		<div className="ui-product-header">
			{isFeatured && <>
				<div className="ui-product-header__featured">
					<i className="fr fr-star"></i>
					{`Recommended`}
				</div>
			</>}

			<h1 className="ui-product-header__title">
				{currentVariant?.name || product.name}
			</h1>

			{role?.canViewAdmin && currentVariant && (product?.notes || currentVariant.notes) && (
				<ProductNotes product={product} variant={currentVariant}/>
			)}

			<ProductStock product={currentVariant || product} />

			{/* display pricing tiers table, if available */}
			<ProductPricing product={currentVariant || product} locale={locale} lessTax={lessTax} />
		</div>
	);
}

const ProductNotes: React.FC<{ product: Product, variant: Product }> = (props) => {
	const { product, variant } = props;

	const [showMoreProductNotes, setShowMoreProductNotes] = useState(false);
	const [showMoreVariantNotes, setShowMoreVariantNotes] = useState(false);

	const MAX_LENGTH = 200;

	const renderNotes = (
		notes: string | undefined | null,
		showMore: boolean,
		setShowMore: React.Dispatch<React.SetStateAction<boolean>>
	) => {
		if (!notes) return null;

		const isLong = notes.length > MAX_LENGTH;
		const displayText = isLong && !showMore ? notes.slice(0, MAX_LENGTH) + "…" : notes;

		return (
			<p className="ui-product-header__note-text">
				{displayText}
				{isLong && (
					<Button className="ui-product-header__show-more" onClick={() => setShowMore((prev) => !prev)}>
						{showMore ? "Show less" : "Show more"}
					</Button>
				)}
			</p>
		);
	};

	return (
		<div className="ui-product-header__notes">
			<h2 className="ui-product-header__notes-title">
				<i className="fr fr-information-circle" />
				{`Notes`}
			</h2>

			{renderNotes(product?.notes, showMoreProductNotes, setShowMoreProductNotes)}
			{renderNotes(variant?.notes, showMoreVariantNotes, setShowMoreVariantNotes)}
		</div>
	);
};

const ProductPricing: React.FC<{ product: Product, locale?: Locale, lessTax?: boolean }> = (props) => {
	const { product, locale, lessTax } = props;
	const priceTiers = getPriceTiers(product);

	if (!priceTiers || priceTiers.length <= 1 || !locale) {
		return;
	}

	const sellUnit = getSellUnit(product);
	const currencyCode = locale?.currencyCode || 'GBP';

	return (
		<table className="table ui-table ui-table--xs ui-product-header__pricing">
			<thead>
				<tr>
					<th>
						{`Quantity`}
					</th>
					<th>
						{`Price ${sellUnit}`}
					</th>
				</tr>
			</thead>
			<tbody>
				{priceTiers.map((tier, i: number) => {
					const isLastTier = (i + 1 == priceTiers.length);
					const from = Math.max(tier.minimumQuantity, 1);
					const to = isLastTier ? ` or more` : priceTiers[i + 1].minimumQuantity - 1;

					return <tr>
						<td>
							{/* prevent "1 - 1" */}
							{from == 1 && to == 1 && <>
								1
							</>}

							{/* \u2014 for em dash */}
							{!(from == 1 && to == 1) && <>
								{`${from}${!isLastTier ? "\u2014" : ""}${to}`}
							</>}
						</td>
						<td>
							{formatCurrency(lessTax ? tier.amountLessTax : tier.amount, { currencyCode })}
						</td>
					</tr>;
				})}
			</tbody>
		</table>
	)
}

export default Header;