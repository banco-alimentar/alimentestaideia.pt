<script lang="ts">
    import type { Option } from '../models';

    export let options:Option[] = [];
    export let selected:string = null;
    export let bigBox:Boolean = false;

    import imagesLib from '../images';
    if(!selected && options)
        selected = options[0].label;

</script>

<div>
{#each options as option, i}
    <div class="box" class:squared={bigBox}>
        <label class="container text dark" class:xsmall={!bigBox} class:medium={bigBox} >
            <input type="radio" checked={ option.label == selected } name="radio" bind:group={selected} value={option.label}>
            <div class="label-container">
                {#if option.image}
                    <div class="image">
                        <svelte:component this={ imagesLib[option.image] }/>
                    </div>
                {/if}
                <span style="opacity:1">{option.label}</span>
            </div>
            <span class="checkmark"></span>
        </label>
    </div>
{/each}
</div>

<style lang="scss">
    @import '../scss/base';

    .box {
        display: inline-flex;
        width: 96px;
        height: 30px;
        border-radius: 15px;
        background-color: $color-white;
        align-items: center;
        padding-left: 8px;
        margin-right: 15px;

        &.squared {
            width: 100%;
            box-sizing: border-box;
            height: 70px;
            border-radius: 10px;
            box-shadow: 2px 2px 11px -2px rgba(0, 0, 0, 0.16);
            background-color: $color-white;
            padding-left: 15px;
            margin-bottom: 20px;
        }
    }

    .container {
        display: block;
        position: relative;
        padding-left: 18px;
        cursor: pointer;
        user-select: none;

        input {
            position: absolute;
            opacity: 0;
            cursor: pointer;
            height: 0;
            width: 0;
        }

        .checkmark {
            position: absolute;
            top: 0;
            left: 0;
            height: 13px;
            width: 13px;
            opacity: 0.2;
            border: solid 1px $color-mid-blue;
            border-radius: 50%;

            &:after {
                content: "";
                position: absolute;
                display: none;
                top: 3px;
                left: 3px;
                height: 7px;
                width: 7px;
                border-radius: 50%;
                background-color: $color-mid-blue;

            }
        }
        
        &:hover input ~ .checkmark {
            opacity: 0.8;
        }

        input:checked ~ .checkmark:after {
            display: block;
        }
        input:checked ~ .checkmark {
            opacity: 1;
        }

        input:checked ~ .label-container .image {
            opacity: 1;
        }
    }
    .image {
        width: 20px;
        display: inline-flex;
        margin: auto 6px;
        opacity: 0.2;
    }

    .label-container {
        display: inline-flex;
    }

    .squared {
        .container {
            .checkmark {
                top: 4px;
            }
        }
    }
</style>