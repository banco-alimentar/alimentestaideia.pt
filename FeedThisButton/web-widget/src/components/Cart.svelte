<script lang="ts">
    import { fade } from 'svelte/transition';
    import { cart } from '../store/Cart';
    import CartItem from './CartItem.svelte';
    export let highlighError: boolean = false;

    const fadeDuration :number = 300;

    let rows :Array<Array<string>> = [];
    let row :Array<string> = []
    let keys = $cart.getItemKeys();
    for (let idx = 0; idx < keys.length; idx++) {
        const key = keys[idx];
        if(idx % 2 === 0) {
            row = [];
            rows.push(row);
        }
        row.push(key);
    }
</script>

<div class="cart">
    <div class="items">
    {#each rows as row }
        {#each row as col}
            <CartItem image={col} item={col}/>
        {/each}
    {/each}
    </div>
    <div class="summary text small dark normal">
        <p class="">O seu carrinho:</p>
        {#if $cart.getTotalAmount()}
            <div in:fade|local={{ duration: fadeDuration, delay: fadeDuration }} out:fade={{ duration: fadeDuration }} >
                {#each $cart.getItemKeys() as item}
                    <p class="item-line" class:disabled={ $cart.getQuantity(item) == 0 }>
                        <span class="text small dark semibold">{ $cart.getLabel(item) }</span>
                        <span class="text xxsmall dark normal" >&nbsp;&nbsp;x&nbsp;&nbsp;{ $cart.getQuantity(item) }</span>
                        <span class="text small dark semibold money">{ $cart.getAmount(item) }€</span>
                    </p>
                    <hr class="spacer"/>
                {/each}
            </div>
        {:else}
            <div in:fade|local={{ duration: fadeDuration, delay: fadeDuration }} out:fade={{ duration: fadeDuration }}>
                <p class="text small dark semibold" class:error={highlighError} style="margin-top:10px;">O seu carrinho ainda está vazio!</p>
                <p class="text small dark semibold" style="margin-top:10px;">Faça a sua doação e ajude o Banco Alimentar a ajudar  os portugueses.</p>
            </div>
        {/if}
    </div>
</div>

<style lang="scss">
    @import '../scss/base';
   
    .error {
        color: $color-red !important;
    }
    .items {
        flex-grow: 0;
        flex-shrink: 1;
        width: 64%;
        display: flex;
        flex-wrap: wrap;
        margin-top: -15px;
        padding-right: 3%;
        justify-content: space-between;
    }

    .cart {
        display: flex;
    }

    .summary {
        display: flex;
        flex-direction: column;
        flex-grow: 1;
        flex-shrink: 0;
        width: 32%;
        padding-top: 1rem;
        padding-left: 1.5rem;
        p, hr {
            margin: 0;
        }
        .disabled {
            opacity: 0.3;
        }
        .item-line {
            margin-top:12px;
            margin-bottom: 0;
        }
        .money {
            float: right;
        }
        .spacer {
            border: solid 1px rgba(69, 69, 69, 0.1);
            margin-top: 4px;
        }
    }


    @media only screen and (max-width: $bp-sm) {
        div.items {
            width: 100%;
            margin-left: auto;
        }
        .summary {
            display: none;
        }
    }
</style>