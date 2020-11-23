<script lang="ts">
    import { customerTypes } from '../store/ReferenceData';
    import { company, name, email, country, securityCode, needsReceipt, receiptEmail, receiptZipCode, receptTaxNumber, acceptedPrivacyPolicy } from '../store/PersonalData';

    import { Country, Option } from '../models';

    import RadioButton from './RadioButton.svelte';
    import InputText from './InputText.svelte';
    import Dropdown from './DropDown.svelte';
    import Checkbox from './Checkbox.svelte';
    import type { text } from 'svelte/internal';
    import { slide } from 'svelte/transition';
    import { ValidatorType } from '../utils/Validators';

    export let highlighError: boolean = false;
</script>

<div style="position:relative">
    <form>
        <div class="type-selector">
            <RadioButton options={customerTypes.map( c => new Option(c))}/>
        </div>
        <div class="row">
            <div class="col1">
                <InputText bind:value={$name} label="Nome" name="name" validator={ValidatorType.Name} highlighError={highlighError}/>
            </div>
        </div>
        <div class="row">
            <div class="col1">
                <InputText bind:value={$email} label="Email" name="email" validator={ValidatorType.Email} highlighError={highlighError}/>
            </div>
        </div>
        <div class="row">
            <div class="col1">
                <InputText bind:value={$country} label="País" name="country" validator={ValidatorType.NotEmpty} highlighError={highlighError}/>
            </div>
        </div>
        <div class="row">
            <div class="col1">
                <Checkbox text="Pretende recibo?" bind:checked={$needsReceipt} />
            </div>
        </div>
        {#if $needsReceipt}
             <div transition:slide|local >
                <div class="row">
                    <div class="col1">
                        <InputText bind:value={$receiptEmail} label="Email" name="receiptEmail" validator={ValidatorType.Email} highlighError={highlighError}/>
                    </div>
                </div>
                <div class="row">
                    <div class="col2-left">
                        <InputText bind:value={$receiptZipCode} label="Código Postal" name="receiptZipCode" validator={ValidatorType.ZipCode} highlighError={highlighError}/>
                    </div>
                    <div class="col2-right">
                        <InputText bind:value={$receptTaxNumber} label="NIF" name="receptTaxNumber" validator={ValidatorType.TaxNumber} highlighError={highlighError}/>
                    </div>
                </div>
             </div>
        {/if}
        <div class="row">
            <div class="col1">
                <Checkbox text="Aceita a Politica de Privacidade e de Protecção de Dados?" 
                    bind:checked={$acceptedPrivacyPolicy} 
                    highlighError={highlighError && !$acceptedPrivacyPolicy}/>
            </div>
        </div>
        <div class="row-spacer"/>
    </form>
</div>

<style lang="scss">
    @import '../scss/base';
    .row {
        display: flex;
        flex-wrap: wrap;
        .col1, .col2-left, .col2-right {
            margin-top: 18px;
        }
    }
    .col1 {
        width: 100%;
    }
    .col2-left {
        width: 46%;
        margin-right:auto;
    }
    .col2-right {
        width: 46%;
        margin-left:auto;
    }
    .row-spacer {
        margin-top: 20px;
    }

    @media only screen and (max-width: $bp-sm) {
        .col2-left, .col2-right {
            width: 100% !important;
        }
    }

</style>