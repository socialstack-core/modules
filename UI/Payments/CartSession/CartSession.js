// Either store your cart locally on a device using this React context or 
// alternatively store it server side using the ShoppingCart content type.
// import { useCart } from 'UI/Payments/CartSession';
// var { addToCart, emptyCart, shoppingCart } = useCart();
// addToCart({product: ProductIdOrObject, quantity: PositiveOrNegativeNumber});
// addToCart({product: ProductIdOrObject, isSubscribing: true}); (adds a quantity of 1)
// To remove either, just addToCart with a negative quantity.
import store from 'UI/Functions/Store';
import webRequest from 'UI/Functions/WebRequest';


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
            qty += (productId == product.id || !productId) ? product.quantity : 0;
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
	
    let addToCart = productInfo => {
        var product = productInfo.product;
        var qty = productInfo.quantity || 0;
        var sub = productInfo.isSubscribing || false;
        if (!product) {
            return;
        }
        if (product.id) {
            product = product.id;
        }
        // Copy item set:
        var curItems = shoppingCart.items;
        var items = [];
        var found = false;
        for (var i = 0; i < curItems.length; i++) {
            var clonedItem = { ...curItems[i] };
            if (clonedItem.product == product) {
                found = true;
                var newQty = clonedItem.quantity + (sub ? qty : qty || 1);
                if (newQty <= 0) {
                    // remove by not re-adding.
                    continue;
                }
                clonedItem.quantity = newQty;
            }
            items.push(clonedItem);
        }
        if (!found) {
            if (qty <= 0 && !sub) {
                // No-op
                return;
            }
            var newItem = {
                product
            };
            if (sub) {
                newItem.quantity = 1;
                newItem.isSubscribing = true;
            } else {
                newItem.quantity = qty;
            }
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
