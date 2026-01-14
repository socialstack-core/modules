import { Product } from 'Api/Product';
import Collapsible from 'UI/Collapsible';
import Html from 'UI/Html';
import ProductSubtitle from 'UI/Product/Subtitle';

/**
 * Props for the FAQs component.
 */
interface FAQsProps {
	/**
	 * Optional section title to display above the downloads.
	 */
	title?: string;

	/**
	 * The product containing FAQs to display.
	 */
	product: Product;

	/**
	 * A selected variant if any.
	 */
	currentVariant?: Product;
}

/**
 * The product FAQs React component.
 * Renders a list of associated product FAQs.
 *
 * @param props React component props.
 */
const FAQs: React.FC<FAQsProps> = ({ title, product, currentVariant }) => {
	const faqsSource = currentVariant || product;

	var faqs = faqsSource.frequentlyAskedQuestionsJson;

	if (!faqs) {
		return;
	}

	faqs = JSON.parse(faqs);

	if (!faqs?.length) {
		return;
	}

	return (
		<div className="ui-product-view__faqs">
			{/* Optional title header */}
			<ProductSubtitle subtitle={title} />

			{/* faqs */}
			{faqs.map((faq) => {
				return <>
					<Collapsible compact title={faq.q} className="ui-product-view__faq">
						<Html>
							{faq.a}
						</Html>
					</Collapsible>
				</>;
			})}
		</div>
	);
};

export default FAQs;
