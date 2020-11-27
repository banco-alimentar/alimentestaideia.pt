<script lang="ts">
    import { fade } from 'svelte/transition';
    import ArrowSvg from '../images/svg/dropdown-arrow.svelte';
    import {clickOutside} from '../utils/ClickOutside';
    type option = {value:string, label:string};

    export let options:Array<option> = [],
        secondary:Boolean = false,
        inline:Boolean = false,
        label:string = 'Escolha uma opção',
        selected:option = null,
        highlighError: boolean = false;
    
    let closed:boolean = true;
    let focused:boolean = false;
    let isGreyColored:boolean = false;

    $: open = !closed;
    $: focused = !!selected;
    $: if(secondary && selected == null) {
        isGreyColored = true;
    } else {
        isGreyColored = false;
    }
    function onSelect(evt:MouseEvent) {
        let element = (evt.target as HTMLElement);
        selected = {value: element.dataset.optionid, label: element.innerText};
    }
    function handleClickOutside(event) {
		if(!closed) closed = true;
	}
</script>

<div class="selector" class:error={highlighError} on:click={()=>{closed = !closed}} class:focused class:greyed={isGreyColored} class:secondary class:input-like={!secondary} class:inline use:clickOutside on:click_outside={handleClickOutside}>
    <p class="label">{label}</p>
    <div class="selected">{selected ? selected.label : ''}</div>

    <ArrowSvg up={!closed} color={secondary && !isGreyColored ? 'blue' : 'grey'}/>
    <div class="separator"/>
    {#if open}
        <div transition:fade={{duration:200}} class="options text xsmall normal" >
            <div class="options-scroll">
                {#each options as option}
                <div class="option" data-optionid={option.value} on:click={onSelect}>{option.label}</div>
                {/each}
            </div>
        </div>
    {/if}
</div>

<style lang="scss">
@import '../scss/base';
.selector {
    width: 100%;
    &:hover {
        cursor: pointer;
    }
    &.inline {
        display: inline-block;
        width: auto;
    }
    &.error {
        color: $color-red !important;
    }
    &.input-like {
        display: block;
        position: relative;
        outline: none;
        margin: 3px 0;
        &.focused .label {
            transition: all .2s linear;
            top: -14px;
            left: 0;
            font-size: 10px;
            color: $color-input-border;
        } 
        .label {
            white-space: nowrap;
            z-index: -1;
            overflow: hidden;
            position: absolute;
            top: 0px;
            display: inline-block;
            background: transparent;
            margin: 7px 0px;
            transition: all .2s linear;
            text-transform: capitalize;
            font-family: OpenSans;
            font-size: $font-size-m;
            font-weight: 600;
            color: $color-greyish-brown;
            padding: 0 0.4em;
        }

        .selected {
            font-family: OpenSans;
            font-size: $font-size-m;
            font-weight: 600;
            color: $color-greyish-brown;
            width: calc(100% - 1.0em);
            padding: 0.4em 0.6em 0.2rem 0em;
            border: none;
            border-bottom: 1px solid $color-input-border;
            background: transparent;
            min-height: 22px;
            &:focus {
                outline-width: 0px;
            }
        }
    }

    &.secondary {
        position: relative;
        color: $color-mid-blue;
        &.greyed {
            color: $color-grey;
            .separator {
                background-color: $color-grey;
            }
        }
        &.focused .label {
            display: none;
        }
        span {
            transform: rotate(90deg);
        }
        .label {
            margin: 0;
            display: inline-block;
        }
        .separator {
            height: 1px;
            border: solid 0px $color-mid-blue;
            background-color: $color-mid-blue;
            margin: 4px 0 0 0;
        }
    }
    .selected, .label {
        padding-right: 15px;
    }
    .options {
        width: calc(100% - 10px);
        min-width: max-content;
        height: 300px;
        padding: 15px 5px;
        position: absolute;
        border-radius: 5px;
        box-shadow: 2px 2px 11px -2px rgba(0, 0, 0, 0.16);
        background-color: $color-mid-blue;
        line-height: 1.8em;
        margin-top: 4px;
        z-index:2;

        .option {
            padding: 4px 8px;
            border-radius: 5px;
            white-space: nowrap;
            &:hover {
                background-color: adjust-color($color-light-blue-grey, $alpha: -0.5);
                cursor: pointer;
            }
            &.closed {
                display: none;
            }
        }
    }
    .options-scroll {
        overflow-y:auto; 
        height:100%;

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
}
</style>