<script lang="ts">
    import imgs from '../images';
    import type { ImageName } from '../models';
    export let alt: boolean = false;
    export let image: ImageName = null;
    export let imageStyle: string = null;
    export let text:string;
    export let large:boolean = false;
    export let responsive:boolean = false;
    export let imageRight :boolean = false;
    export let disabled :boolean = false;

    let imageComponent = imgs[image];

</script>

<template>
    <div on:mouseenter on:mouseleave>
        <button class="btn-rounded" type="button" class:alt class:responsive class:large class:disabled on:click disabled={disabled}>
            <slot>
                {#if imageRight}
                    <div class="text" class:alt={!alt} >{text}</div>
                    <div class="img" class:alt class:right={true} style={imageStyle}>
                        <svelte:component this={ imageComponent } style="display:inline"/>
                    </div>
                {:else}
                    <div class="img" class:alt style={imageStyle}>
                        <svelte:component this={ imageComponent } style="display:inline"/>
                    </div>
                    <div class="text" class:alt={!alt} >{text}</div>
                {/if}
            </slot>
        </button>
    </div>
</template>

<style type="text/scss">
    @import '../scss/base';

    

    .btn-rounded {
        border-radius: 25px;
        padding: 4px 11px;
        box-shadow: 0 2px 5px 0 rgba(0, 0, 0, 0.23);
        background-color: $color-white;
        align-content: center;
        display: inline-flex;
        align-items: center;
        white-space: nowrap;
        color: $color-mid-blue;
        margin: 0;
        border: 0;
        &:disabled {
            opacity: 0.5;
        }
        &.large {
            padding: 10px 17px;
        }
        &.alt {
            background-color: $color-mid-blue;
            color: $color-white;
        }
        &:hover{
            cursor: pointer;
        }
        &:focus {
            outline: none;
            box-shadow: 0 0 0 1pt adjust-color($color: $color-mid-blue, $alpha: -0.5);
        }

        .img {
            margin-right: 5px;
            fill: $color-mid-blue;

            &.alt {
                fill: $color-white;
            }

            &.right {
                margin-left: 15px;
                margin-right: 0px;
            }
        }
        
        @media only screen and (min-width: $bp-lg) and (max-width: $bp-xl)  {
            &.responsive {
                padding: 10px 17px;
            }
        }
    }
</style>

