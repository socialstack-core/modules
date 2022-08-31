// Either store your cart locally on a device using this React context or 
// alternatively store it server side using the ShoppingCart content type.
// import { useCart } from 'UI/Payments/CartSession';
// var { addToCart, emptyCart, shoppingCart } = useCart();
// addToCart({product: ProductIdOrObject, quantity: PositiveOrNegativeNumber});
// addToCart({product: ProductIdOrObject, isSubscribing: true}); (adds a quantity of 1)
// To remove either, just addToCart with a negative quantity.
import store from 'UI/Functions/Store';

const CartSession = React.createContext();

export const Provider = (props) => {
    const [shoppingCart, setShoppingCart] = React.useState(() => {
        var cart = store.get('shopping_cart');
        if (!cart || !cart.items) {
            cart = { items: [] };
        }
        return cart;
    });

    // return quantity of given product within cart (or all products if productId == null)
    let getCartQuantity = (productId) => {
        var qty = 0;

        shoppingCart.items.forEach(product => {
            qty += (productId == product.product || !productId) ? product.quantity : 0;
        });

        return qty;
    }

    let hasSubscriptions = () => {
        var result = false;

        shoppingCart.items.forEach(product => {

            if (product.isSubscribing) {
                result = true;
            }

        });

        return result;
    }

    let cartIsEmpty = () => {
        return !shoppingCart.items.length;
    };

    let updateCart = (newCart) => {
        store.set('shopping_cart', newCart);
        setShoppingCart(newCart);
    };
	
	let removeSubscriptions = () => {
		
		var newItemSet = shoppingCart.items.filter(product => {
            if(product.isSubscribing) {
                return;
            }
			
			return product;
        });
		
		updateCart(newItemSet);
	};
	
    let addToCart = (productInfo, options) => {
            var product = productInfo.product;
            var qty = productInfo.quantity || 1;
			
            if (!product) {
				return;
            }
			
            if (product.id) {
                product = product.id;
            }
			
            // Copy item set:
            var curItems = shoppingCart.items;
			
			if(options && options.itemFilter){
				curItems = options.itemFilter(curItems);
			}
			
            var items = [];
            var found = false;

            for (var i = 0; i < curItems.length; i++) {
                var clonedItem = { ...curItems[i] };
                if (clonedItem.product == product) {
                    found = true;
                    var newQty = clonedItem.quantity + qty;
                    if (newQty <= 0) {
                        // remove by not re-adding.
                        continue;
                    }
                    clonedItem.quantity = newQty;
                }
                items.push(clonedItem);
            }
			
            if (!found) {
				if(qty <= 0){
					return;
				}
				
                var newItem = {
                    product
                };
                newItem.quantity = qty;
                items.push(newItem);
            }

            updateCart({ items });
    }
	
    let emptyCart = () => {
        updateCart({ items: [] });
    };

    return (
        <CartSession.Provider
            value={{
                shoppingCart,
                addToCart,
				removeSubscriptions,
                emptyCart,
                cartIsEmpty,
                getCartQuantity,
                hasSubscriptions
            }}
        >
            {props.children}
        </CartSession.Provider>
    );
};

export { CartSession };

export function useCart() {
    return React.useContext(CartSession);
}
