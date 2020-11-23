<script lang="ts">
    import { onMount } from 'svelte';
    export let up:boolean = false, color:"blue" | "grey" = "blue";
    onMount(async () => {
		console.log(color);
    });
    $: blue = color == "blue";
    $: grey = color == "grey";
</script>

<svg class:up class:blue class:grey width="9" height="6" xmlns="http://www.w3.org/2000/svg"  stroke-width="2">
    <line y1="5" x1="4.35" y2="0" x2="9"/>
    <line y2="5" x2="4.65" y1="0" x1="0"/>
</svg>

<style lang="scss">
    @import '../../scss/base';

    svg {
        position: absolute;
        top: 50%;
        transform: translateY(-50%);
        right: 0%;
        transition: transform 0.2s;
        &.blue {
            stroke: $color-mid-blue;
        }
        &.grey {
            stroke: $color-grey;   
        }
        &.up {
            transform: rotate(180deg);
        }
    }
</style>