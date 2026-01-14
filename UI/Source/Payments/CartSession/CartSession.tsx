import { CartItemChange } from 'Api/Purchase';
import shoppingCartApi, { ShoppingCart, GuestDetails} from 'Api/ShoppingCart';
import { createContext, useContext, useState, useEffect, useMemo } from 'react';
// import { useCart } from 'UI/Payments/CartSession';
// var { addToCart, emptyCart, shoppingCart } = useCart();
// addToCart({product: ProductIdOrObject, quantity: PositiveOrNegativeNumber});
// addToCart({product: ProductIdOrObject, isSubscribing: true}); (adds a quantity of 1)
// To remove either, just addToCart with a negative quantity.
import store from 'UI/Functions/Store';
import { ApiIncludes } from "Api/Includes";
import { ProductQuantityPricing } from 'Api/Content';

interface CartContext {
    addToCart?: (productId: uint, quantity: int, isDelta: boolean) => void;
    addGuestDetails?: (guestDetails: GuestDetails) => void;
    shoppingCart?: ShoppingCart,
    loading?: boolean,
    lessTax?: boolean,
    emptyCart?: () => void,
    setLessTax?: (lessTax: boolean) => void,
    cartIsEmpty?: () => boolean,
    hasSubscriptions?: () => boolean,
    cartIsDigitalOnly?: () => boolean,
    setCoupon?: (coupon: string) => Promise<void>,
    getCartQuantity?: (productId: uint) => number,
    getCartId?: () => CartIdToken,
    cartContents?: ProductQuantityPricing,
    includeSet: ApiIncludes[],
    replaceCart: (cart: ShoppingCart) => void
}

type CartIdToken = {
    id: uint,
    anonKey?: string
};

const CartSession = createContext<CartContext>({
});

export const Provider: React.FC<React.PropsWithChildren> = (props) => {
    const [shoppingCart, setShoppingCart] = useState<ShoppingCart | null>(null);
    const [loading, setLoading] = useState<boolean>(true);
    const [lessTax, setLessTaxLocal] = useState<boolean>(true);

    const setLessTax = (val: boolean) => {
        setLessTaxLocal(val);
        store.set("less_tax", val);
    };

    const includeSet: ApiIncludes[] = useMemo(() => [
        shoppingCartApi.includes.productquantities,
        shoppingCartApi.includes.productquantities.product,
        shoppingCartApi.includes.productquantities.product.primaryCategory,
        shoppingCartApi.includes.productquantities.productnotices,
        shoppingCartApi.includes.cartcontents,
        shoppingCartApi.includes.coupon
    ], []);

    const setCouponInternal = (coupon: string) => {
        if (!coupon) {
            return shoppingCartApi.removeCoupon({
                shoppingCartId: shoppingCart!.id,
                anonymousCartKey: shoppingCart!.anonymousCartKey,
            }, includeSet);
        }
        return shoppingCartApi.applyCoupon({
            shoppingCartId: shoppingCart!.id,
            anonymousCartKey: shoppingCart!.anonymousCartKey,
            code: coupon
        }, includeSet);
    };

    const setCoupon = (coupon: string) => {
        return setCouponInternal(coupon)
            .then(loadCart);
    };
	
    const getCartId = (): CartIdToken => {
		if(shoppingCart){
            return {
                id: shoppingCart.id,
                anonKey: shoppingCart.anonymousCartKey
            };
		}
		
        var cartIdToken = store.get('shopping_cart_ref') as CartIdToken;
		
        if (!cartIdToken){
            return {
                id: 0 as uint,
                anonKey: undefined
            };
		}
		
        return cartIdToken;
	};
	
    const loadCart = (cart: ShoppingCart) => {
        // Merge productQuants in to contents.
        var { productQuantities, cartContents } = cart;

        if (cartContents && cartContents.contents && productQuantities) {

            var contents = cartContents.contents;

            var variantParents = new Map<uint, uint>();

            for (var i = 0; i < contents.length; i++) {
                var lineItem = contents[i];
                const pq = productQuantities.find(pq => pq?.id == lineItem.productQuantityId);
                const product = pq?.product;

                lineItem.productQuantity = pq;
                lineItem.product = product;

                if (!product) {
                    continue;
                }
				
                // relocate productQuantity notices -> the product.
                product.productNotices = pq.productNotices;
            }

        }

        setShoppingCart(cart);
    };
    
    useEffect(() => {
        var lessTax = store.get("less_tax") as boolean;

        if (lessTax !== undefined && lessTax !== null) {
            setLessTaxLocal(lessTax);
        }

        var cartRef = store.get('shopping_cart_ref') as CartIdToken;

        if (cartRef) {
            shoppingCartApi.loadAnon(cartRef.id, cartRef.anonKey || '', includeSet)
                .then(response => {
                    loadCart(response);
                    setLoading(false);
                }).catch(e => {
                    console.error(e);
                    setLoading(false);
                })
        } else {
            setLoading(false);
        }
    }, [includeSet]);

    // return quantity of given product within cart (or all products if productId == null)
    let getCartQuantity = (productId: uint) => {
        var qty = 0;

        shoppingCart?.productQuantities?.forEach(product => {
            if (!productId || productId == product?.productId) {
                qty += product?.quantity || 0;
            }
        });

        return qty;
    }

    let hasSubscriptions = () => {
        var result = false;

        shoppingCart?.productQuantities?.forEach(productQty => {
            var product = productQty?.product;
            if (product?.billingFrequency != 0 || product?.isBilledByUsage) {
                result = true;
            }
        });

        return result;
    }

    let cartIsDigitalOnly = () => {
        var result = true;

        shoppingCart?.productQuantities?.forEach(productQty => {
            var product = productQty?.product;
            if (product?.productType === 0) {
                result = false;
            }
        });

        return result;
    };

    let cartIsEmpty = () => {
        return !shoppingCart?.productQuantities?.length;
    };

    let addToCart = (productId: uint, quantity: int, isDelta: boolean = false) => {

        var items : CartItemChange[] = [
            {
                productId,
                quantity: isDelta ? undefined : quantity,
                deltaQuantity: isDelta ? quantity : undefined
            }
        ];

        return shoppingCartApi.changeItems({
            shoppingCartId: shoppingCart?.id || (0 as uint),
            anonymousCartKey: shoppingCart?.anonymousCartKey,
            items
        }, includeSet).then(cart => {
            store.set('shopping_cart_ref', { id: cart.id, anonKey: cart.anonymousCartKey });
            loadCart(cart);
            return cart;
        }).catch((e: PublicError) => {
            if (e?.type && e.type == 'cart/not_found') {
                console.log("Removing old cart reference - try adding again.");
                store.remove('shopping_cart_ref');
                setShoppingCart(null);
            } else {
                // rethrow
                throw e;
            }
        });
    }

    let addGuestDetails = (guestDetails: GuestDetails) => {
        return shoppingCartApi.updateGuest(
            shoppingCart?.id || (0 as uint),
            shoppingCart?.anonymousCartKey,
            guestDetails, 
            includeSet
        ).then(cart => {
            store.set('shopping_cart_ref', { id: cart.id, anonKey: cart.anonymousCartKey });
            loadCart(cart);
            return cart;
        }).catch((e: PublicError) => {
            if (e?.type && e.type == 'cart/not_found') {
                console.log("Removing old cart reference - try adding again.");
                store.remove('shopping_cart_ref');
                setShoppingCart(null);
            } else {
                // rethrow
                throw e;
            }
        });
    }


    // Advanced usage: The cart must have the full includeSet.
    let replaceCart = (cart?: ShoppingCart) => {
        if (!cart) {
            store.remove('shopping_cart_ref');
            setShoppingCart(null);
            return;
        }

        store.set('shopping_cart_ref', { id: cart.id, anonKey: cart.anonymousCartKey });
        loadCart(cart);
    };

    let emptyCart = () => {
        store.remove('shopping_cart_ref');
        setShoppingCart(null);
        setLoading(false);
    };
	
	// For state hydration with SSR, the basket needs to be empty first 
	// render to avoid inflation issues. This state and its succeeding 
	// useEffect are both mechanics in a run once op.
	const [nullFirstRender, setNullFirstRender] = useState(false);

	useEffect(() => {
		if (!nullFirstRender) {
			setNullFirstRender(true);
		}
	}, []);

    return (
        <CartSession.Provider
            value={{
                shoppingCart: nullFirstRender ? (shoppingCart || undefined) : undefined,
                addToCart,
                addGuestDetails,
                loading,
                emptyCart,
                cartIsEmpty,
                getCartQuantity,
                hasSubscriptions,
                lessTax,
                setLessTax,
                cartIsDigitalOnly,
                setCoupon,
                getCartId,
                includeSet,
                replaceCart,
                cartContents: nullFirstRender ? (shoppingCart?.cartContents) : undefined
            }}
        >
            {props.children}
        </CartSession.Provider>
    );
};

export { CartSession };

export function useCart() {
    return useContext(CartSession) || {lessTax: true};
}
