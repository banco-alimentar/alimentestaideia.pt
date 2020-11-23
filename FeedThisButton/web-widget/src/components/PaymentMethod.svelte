<script>
import { Option } from '../models';
import { errorMessage } from '../store/Global';

import RadioButton from "./RadioButton.svelte";

let radioOptions = [
    new Option("Multibanco", "multibanco"),
    //new Option("MB Way", "mbway", true),
]
</script>
<div class="container">
    <div class="selector">
        <RadioButton
            options={ radioOptions }
            bigBox/>

        {#if $errorMessage}
        <div class="text small error normal">
            <p class="">Ocorreu um erro, por favor tente novamente mais tarde</p>
        </div>
        {/if}
    </div>
    <div class="summary text small dark normal">
        <p class="">Faça a sua doação de forma segura!</p>
        <p class="text small dark semibold" style="margin-top:10px;">Assim que for recebido o seu pagamento, será enviado para o seu email o recibo da doação realizada.</p>
        <p>&nbsp;</p>
    </div>
</div>

<style lang="scss">
    @import '../scss/base';
    .container {
        display: flex;
    }
    .selector {
        flex-grow: 0;
        flex-shrink: 1;
        width: 66%;
    }
    .summary {
        display: flex;
        flex-direction: column;
        flex-grow: 1;
        width: 33%;
        margin-left: 15px;
        p, hr {
            margin: 0;
        }
    }


    @media only screen and (max-width: $bp-sm) {
        .selector {
            width: 100% !important;
        }
        .summary {
            display: none;
        }
    }
</style>