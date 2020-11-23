<script lang="ts">
    import LogoMultibanco from '../images/svg/logo-multibanco.svelte';
    import { entity, reference, amount } from '../store/Multibanco';
</script>

<div class="container">
    <div class="data">
        <p class="text medium dark semibold">Para concluir a sua doação utilize a referência abaixo.</p>
        <p class="text medium dark normal">Esta referência foi também enviada para o seu email.</p>
        <hr class="separator">
        <div class="mb-summary">
            <div class="logo">
                <LogoMultibanco />
            </div>
            <div class="details">
                <p class="text small dark semibold">ENTIDADE: <span class="text medium bold dark">{$entity}</span></p>
                <p class="text small dark semibold">REFERENCIA: <span class="text medium bold dark">{$reference}</span></p>
                <p class="text small dark semibold">VALOR: <span class="text medium bold dark">{$amount} €</span></p>
            </div>
        </div>
    </div>
    <div class="summary text small dark normal">
       
    </div>
</div>

<style lang="scss">
    @import '../scss/base';
    .container {
        display: flex;
    }
    .data {
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
    .separator {
        border: solid 1px adjust-color($color: $color-grey, $alpha: -0.5);
        margin-top: 40px;
        margin-bottom: 30px;
    }

    .mb-summary {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        justify-content: center;
        .logo {
            width: 96px;
        }
    }

    @media only screen and (max-width: $bp-sm) {
        .data {
            width: 100% !important;
        }
        .summary {
            display: none;
        }
    }
</style>