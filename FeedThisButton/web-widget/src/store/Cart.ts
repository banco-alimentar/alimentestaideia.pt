import { writable, derived, Writable } from 'svelte/store';
import { ShoppingCart } from '../models';

function createShoppingCart(){
    const { subscribe, set, update } = writable(new ShoppingCart());

    function addItem(item:string) {
        return update(state => {
            state.items[item].quantity++;
            return state;
        });
    }
    function removeItem(item:string) {
        return update(state => {
            if(state.items[item].quantity > 0) {
                state.items[item].quantity--;
            }
            return state;
        });
    }
    return {
        subscribe,
        addItem,
        removeItem
    }
}
// not realy a store but we don't need inter-component reactivity, so a poor man's store
export const cart = createShoppingCart();
export const foobBankToDonate:Writable<{value:string, label:string}> = writable(null);
export const isShoppingCartComplete = derived(
    [cart, foobBankToDonate],
    ([$cart, $foobBankToDonate]) => $cart.getTotalAmount() > 0 && $foobBankToDonate != null && $foobBankToDonate != null
);