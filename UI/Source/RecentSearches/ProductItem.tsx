import { Product } from "Api/Product";
import Image from 'UI/Image';
import Link from 'UI/Link';
import ProductQuantity from "UI/Payments/ProductQuantity";
import defaultImageRef from './image_placeholder.png';

/**
 * Props for the `RecentSearchProductItem` component.
 */
export type RecentSearchProductItemProps = {
    /**
     * The product object representing the recently searched item.
     */
    product: Product;

    /**
     * (Optional) Quantity of the product that already exists in the cart.
     * Defaults to `0` if not provided.
     */
    existingQty?: number;

    /**
     * (Optional) Callback function to trigger when the product is added to the cart.
     * Accepts the product and the selected quantity as arguments.
     */
    onAddToCart?: (product: Product, qty: number) => void;
};

/**
 * A UI component that displays a single recently searched product.
 *
 * This component is typically used inside a list or carousel of recently searched products,
 * and provides basic display information like the product image, name, and a quantity selector.
 *
 * ### Features:
 * - Displays a placeholder image (can be replaced by actual product images in future).
 * - Shows the product name.
 * - Renders a quantity selector for potential cart interactions.
 *
 * ### Usage:
 * ```tsx
 * <RecentSearchProductItem
 *   product={product}
 *   existingQty={2}
 *   onAddToCart={(product, qty) => handleAdd(product, qty)}
 * />
 * ```
 *
 * @component
 * @param {RecentSearchProductItemProps} props - The props containing product data and optional cart functions.
 * @returns {React.ReactElement} A visual representation of a single recent product item.
 */
const RecentSearchProductItem: React.FC<RecentSearchProductItemProps> = (
    props: RecentSearchProductItemProps
): React.ReactElement => {

    const { product, existingQty = 0, onAddToCart } = props;
    
    return (
        <div className={'recent-searches-product-item'}>
            <Link
                href={product.slug!}
            >
                {/* Product thumbnail (placeholder) */}
                <Image fileRef={defaultImageRef} />
    
                {/* Product name, potentially truncated */}
                <p>{product.name}</p>
            </Link>
            {/* Quantity selector (display only; no handler wired up yet) */}
            <ProductQuantity
                product={product}
                quantity={existingQty}
            />
        </div>
    );
};

export default RecentSearchProductItem;
