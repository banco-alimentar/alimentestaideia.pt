<script lang="ts">
    import { getValidator, ValidatorType } from "../utils/Validators";
    export let 
        value: string = '', 
        label: string = '', 
        name: string = '', 
        error: boolean = false, 
        validator: ValidatorType = ValidatorType.None,
        maxlength: number = null,
        highlighError: boolean = false;

    let focused:boolean = value != null && value.length > 0;
    const onFocus =()=> {
        focused = true;
    }

    let validatorFunc = getValidator(validator);
	const onBlur =()=> {
        if(value == null || value.length == 0) {
            focused = false;
        } else {
            error = !validatorFunc(value);
        }
    };


    const validateInput = () => {
        error = false;
    };
</script>

<div class="field-wrapper" class:focused={focused}  >
    <input type="text" on:focus={onFocus} on:blur={onBlur} bind:value={value} name={name} autocomplete={name} class:error on:input={ validateInput } maxlength={ maxlength }>
    <label for={name} class:error= {error || (highlighError && !validatorFunc(value))} >{label}</label>
</div>

<style lang="scss">
    @import '../scss/base';

    .field-wrapper {
        display: block;
        margin: 0px;
        position: relative;
        outline: none;
        width: 100%;
        &.focused > label, &:hover > label {
            transition: all .2s linear;
            top: -14px;
            font-size: 10px;
            color: $color-input-border;
        } 
        label {
            white-space: nowrap;
            z-index: 1;
            overflow: hidden;
            position: absolute;
            top: 0px;
            left: 0;
            display: inline-block;
            background: transparent;
            margin: 7px 0px;
            padding: 0;
            transition: all .2s linear;
            text-transform: capitalize;
            font-family: OpenSans;
            font-size: $font-size-m;
            font-weight: 600;
            color: $color-greyish-brown;
            &.error {
                color: $color-red;
            }
        }

        input {
            font-family: OpenSans;
            font-size: $font-size-m;
            font-weight: 600;
            color: $color-greyish-brown;
            width: calc(100% - 0.8em);
            padding: 0.4em;
            border: none;
            border-bottom: 1px solid $color-input-border;
            background: transparent;
            &:focus {
                outline-width: 0px;
            }
            &.error {
                border-color: $color-red;
            }
        }
    }
    input:-webkit-autofill {
        transition: background-color 5000000s;
        font-family: OpenSans;
        font-size: $font-size-m;
        font-weight: 600;
        color: $color-greyish-brown;
        &,
        &:hover, 
        &:focus, 
        &:active  {
            transition: background-color 5000000s;
            font-family: OpenSans;
            font-size: $font-size-m;
            font-weight: 600;
            color: $color-greyish-brown;
            & + label {
                transition: all .2s linear;
                top: -14px;
                font-size: 10px;
                color: $color-input-border;
            }
        }
        & + label {
            transition: all .2s linear;
            top: -14px;
            font-size: 10px;
            color: $color-input-border;
        }
    }

</style>
