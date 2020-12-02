<script lang="ts">
    import { NavigationStep} from '../models';
    import { cart, foobBankToDonate, isShoppingCartComplete } from '../store/Cart';
    import { isPersonalDataFormValid } from '../store/PersonalData';
    import { loading, errorMessage } from '../store/Global';
    import { foodBanksList } from '../store/ReferenceData';
    import { generateMultibancoReference } from '../services';

    import DropDown from './DropDown.svelte';
    import Cart from './Cart.svelte';
    import Notch from '../images/svg/Bellcurve.svelte';
    import CartItem from './CartItem.svelte';
    import LogoBa from '../images/svg/logo-ba.svelte';
    import RoundButton from './RoundButton.svelte';

    import { fade } from 'svelte/transition';
    import PersonalData from './PersonalData.svelte';
    import PaymentMethod from './PaymentMethod.svelte';
    import MultibancoConfirmation from './MultibancoConfirmation.svelte';
    import Spinner from './Spinner.svelte';

    export let closed:boolean = false;

    let highlightActionsNeeded: boolean = false;
    let navigationStep:NavigationStep = NavigationStep.ShoppingCart;
    let continueButtonDisabled = false;
    $: {
        if( navigationStep == NavigationStep.ShoppingCart)
            continueButtonDisabled = !$isShoppingCartComplete;
        else if( navigationStep == NavigationStep.PersonalData)
            continueButtonDisabled = !$isPersonalDataFormValid;
        else
            continueButtonDisabled = false;
    }

    // clear errors on modal
    $: $errorMessage = navigationStep ? null : null;

    let nextButtonText: string = 'Continuar';
    $: nextButtonText = navigationStep == NavigationStep.MultibancoConfirmation ? 'Nova doação' : 'Continuar';

    // Errors highlight 
    let highlightDropdown: boolean = false,
        highlightCart: boolean = false,
        highlightPersonalData: boolean = false;
    $: highlightDropdown = navigationStep == NavigationStep.ShoppingCart && highlightActionsNeeded && !$foobBankToDonate;
    $: highlightCart = navigationStep == NavigationStep.ShoppingCart && highlightActionsNeeded && $cart.isEmpty();
    $: highlightPersonalData = navigationStep == NavigationStep.PersonalData && highlightActionsNeeded;
    async function onContinue() {
        if(navigationStep == NavigationStep.PaymentMethod) {
            try {
                await generateMultibancoReference();
                navigationStep = NavigationStep.MultibancoConfirmation;
            } catch(e) {
                console.log(e);
                $errorMessage = 'Ocorreu um erro, por favor tente mais tarde';
            }
        } else {
            navigationStep >= 4 ? navigationStep = 1 : navigationStep++
        }
    }
</script>

{#if !closed}
<div class="modal-outter">
    <div class="modal" transition:fade>
        <div class="close-btn" on:click={() => { closed = true; navigationStep = NavigationStep.ShoppingCart; }}>
            <Notch />
            <div class="close-icon"> 
                <div class="left-x">
                    <div class="right-x "> </div>
                </div>
            </div>
        </div>
        <div class="modal-inner">
            <div class="scroll">
                <div class="header">
                    <p class="text xsmall alt title" style="line-height:1.8">BANCO ALIMENTAR</p>
                    <div class="switcher-container">
                        {#if navigationStep === NavigationStep.ShoppingCart}
                            <div in:fade|local={{duration: 300, delay:300}} out:fade|local={{duration: 300}} class="switcher-item">
                                <span class="text large bold dark subtitle" style="line-height:1.1">Quero doar ao Banco Alimentar de </span><span class="text large bold dark subtitle line2"><DropDown secondary={true} inline={true} options={foodBanksList} bind:selected={$foobBankToDonate} highlighError={highlightDropdown} label="Escolha a região"/> estes bens alimentares:</span>
                            </div>
                        {:else if  navigationStep === NavigationStep.PersonalData}
                            <div in:fade|local={{duration: 300, delay:300}} out:fade|local={{duration: 300}} class="switcher-item">
                                <span class="text large bold dark subtitle" style="line-height:1.1">Preencha os seus dados</span>
                            </div>
                        {:else if  navigationStep === NavigationStep.PaymentMethod}
                            <div in:fade|local={{duration: 300, delay:300}} out:fade|local={{duration: 300}} class="switcher-item">
                                <span class="text large bold dark subtitle" style="line-height:1.1">Preencha os seus dados</span>
                            </div>
                        {:else if  navigationStep === NavigationStep.MultibancoConfirmation}
                            <div in:fade|local={{duration: 300, delay:300}} out:fade|local={{duration: 300}} class="switcher-item">
                                <span class="text large bold dark subtitle" style="line-height:1.1">Desde já o nosso obrigado em nome do Banco Alimentar e dos Portugueses!</span>
                            </div>
                        {/if} 
                    </div>
                </div>
                <div class="items switcher-container">
                    {#if navigationStep === NavigationStep.ShoppingCart}
                        <div  in:fade|local={{duration: 300, delay:300}} out:fade|local={{duration: 300}} class="switcher-item">
                            <Cart highlighError={highlightCart}/>
                        </div>
                    {:else if  navigationStep === NavigationStep.PersonalData}
                        <div in:fade|local={{duration: 300, delay:300}} out:fade|local={{duration: 300}} class="switcher-item">
                            <PersonalData highlighError={highlightPersonalData}/>
                        </div>
                    {:else if  navigationStep === NavigationStep.PaymentMethod}
                        <div in:fade|local={{duration: 300, delay:300}} out:fade|local={{duration: 300}} class="switcher-item">
                            <PaymentMethod />
                        </div>
                    {:else if  navigationStep === NavigationStep.MultibancoConfirmation}
                        <div in:fade|local={{duration: 300, delay:300}} out:fade|local={{duration: 300}} class="switcher-item">
                            <MultibancoConfirmation />
                        </div>
                    {/if}
                </div>
            </div>
            <div class="footer">
                {#if navigationStep !== NavigationStep.MultibancoConfirmation}
                <div class="total">
                    <span class="text xsmall normal dark sp225">TOTAL: </span>
                    <span class="text mediumlg bold dark ">{$cart.getTotalAmount()}€</span>
                </div>
                {/if}
                <div style="width:100%; display:flex; justify-content: space-between">
                    <div class="logo">
                        <LogoBa />
                    </div>
                    <div class="links text xsmall alt">
                        <a href="https://www.bancoalimentar.pt/politica-de-privacidade-e-protecao-de-dados/" target="_blank" rel="noopener noreferrer">Política de privacidade</a>
                    </div>
                    <div class="btn">
                        <RoundButton responsive={false} alt large text={nextButtonText} image="seta" imageRight 
                            on:click={onContinue} disabled={continueButtonDisabled}
                            on:mouseenter={ () => {highlightActionsNeeded = continueButtonDisabled;} }
                            on:mouseleave = { () => {highlightActionsNeeded = false;} }
                            />
                    </div>
                </div>
            </div>
        </div>
        <Spinner display={ $loading }/>
    </div>
</div>
{/if}

<style lang="scss">
    @import '../scss/base';

    .modal div{
        text-align: left;
    }
    .switcher-container {
        display: grid;
        grid-template-rows: 1fr;
        grid-template-columns: 1fr;
        align-items: start;
    }
    .switcher-item {
        grid-column: 1;
        grid-row: 1;
    }

    .text {
        &.title {
            padding: 5px 0;
        }
        &.subtitle {
            line-height: 1.1;
            letter-spacing: 0.11px;
            &.line2 {
                margin-top: 5px;
                display: block;
            }
        }
    }

    .modal-outter {
        position: fixed;
        top: 50px;
        left: 50%;
        transform: translate(-50%, 0);
        width: 100%;
        max-width: 620px;
        height: calc(100% - 50px);
        max-height: 720px;
        z-index: 9999;
    }
    .modal-inner {
        display: block;
        position: relative;
        padding: 40px 40px 0 40px;
        overflow: hidden;
        height: 100%;
    }
    .modal {
        z-index: 1010;
        margin: 0px 20px 0px 40px;
        border-radius: 10px;
        box-shadow: 16px 26px 83px 3px $color-brownish-grey-50;
        background-color: rgba(243, 244, 249, 0.9);
        background-image: url('images/ba-logo-background.svg') ;
        background-size: cover;
        position: relative;
        height: 100%;

        .items {
            margin-top: 25px;
        }

        p {
            margin: 0;
        }
        &.closed {
            display: none;
        }

        .close-btn {
            transform: rotate(-90deg);   
            position: absolute;
            left: -60px;
            width: 100px;
            fill: rgba(243, 244, 249, 0.9);
            top: 40px;
        }
        .close-icon {
            position: absolute;
            margin-left: 35px;
            top:5px;
            height: 30px;
            width: 30px;
            border-radius: 25px;
            &:hover {
                cursor: pointer;
                background: radial-gradient(adjust-color($color: $color-mid-blue, $alpha: -0.8), $color-white);
            }
            .left-x {
                height: 15px;
                width: 2px;
                margin: 6px auto;
                background-color: $color-mid-blue;
                transform: rotate(45deg);
                z-index: 1;
            }

            .right-x {
                position: relative;
                height: 15px;
                width: 2px;
                background-color: $color-mid-blue;
                transform: rotate(90deg);
                z-index: 2;
            }
        }
    }

    .scroll {
        overflow-y: auto;
        overflow-x: hidden; 
        height: inherit; 
        max-height: 600px;
        height: calc(100% - 150px);
        &::-webkit-scrollbar {
            width: 12px;
        }
        &::-webkit-scrollbar-track {
            -webkit-box-shadow: inset 0 0 6px rgba(0,0,0,0); 
            border-radius: 10px;
        }

        &::-webkit-scrollbar-thumb {
            border-radius: 10px;
            -webkit-box-shadow: inset 0 0 6px rgba(0,0,0,0.3); 
        }
    }

    .footer {
        width: inherit;
        max-width: 100%;
        box-sizing: content-box;
        margin-top: auto;
        .total {
            width: max-content;
            margin-left: auto ;
            margin-bottom: 10px;
        }

        .links {
            display: inline-block;
            margin-bottom: 2.5em;
            margin-left: 1em;
            align-self: flex-end;
            a {
                text-decoration: none;
                color: $color-mid-blue;
            }
        }

        .logo {
            display: inline-block;
            width: 40px;
            fill: adjust-color($color: $color-mid-blue, $alpha: -0.8);
            margin-left:-30px;
            margin-bottom: 10px;
        }
        .btn {
            align-self: end;
            margin: 0 ;
        }
    }

    @media only screen and (max-height: 720px) {
        .modal-outter {
            top: 0;
            height: 100%;
        }
    }
    @media only screen and (max-width: $bp-sm) {
        .modal-inner {
            padding: 20px 20px 0 20px;
        }
        .modal {
            margin-left: 20px;
        }
        .modal-outter {
            top: 0;
            height: 100%;
        }
        .scroll {
            height: calc(100% - 100px);
        }
    }
</style>