import Loading from 'UI/Loading';
import { useCart } from 'UI/Payments/CartSession';
import ProductTable from 'UI/Payments/ProductTable';
import CartTotal from 'UI/Payments/CartTotal';
import PromoCode from 'UI/Payments/PromoCode';
import Badge from 'UI/Badge';

/**
 * Props for the Cart component.
 */
interface CartProps {
	/**
	 * associated title
	 */
	title?: string,

	customFooter?: React.ReactNode
}

/**
 * The Cart React component.
 * @param props React props.
 */
const Cart: React.FC<CartProps> = (props) => {
	const { customFooter } = props;
	const title = props.title?.length ? props.title : `Shopping Basket`;
	var { addToCart, emptyCart, shoppingCart, cartIsEmpty, loading, lessTax } = useCart();

	var cartEmpty = loading || (cartIsEmpty ? cartIsEmpty() : true);

	return <>
		<div className="shopping-cart">
			<h1 className="shopping-cart__title">
				{title}
				{shoppingCart && shoppingCart.reference &&
				<>
					<Badge variant="secondary">
						{shoppingCart.reference}
					</Badge>
				</>}
			</h1>

			{loading && <Loading />}

			{!loading && <>
				<ProductTable shoppingCart={shoppingCart!} lessTax={lessTax} />

				{!cartEmpty && (customFooter || <>
					<div className="shopping-cart__internal">
						<PromoCode shoppingCart={shoppingCart!} />
						<CartTotal shoppingCart={shoppingCart!} emptyCart={emptyCart} />
					</div>
				</>)}

			</>}
		</div>
	</>;
}

export default Cart;