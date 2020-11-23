<!--
    Taken from https://github.com/Schum123/svelte-loading-spinners
-->
<script lang="ts">
    export let display:boolean = false;
    export let size = 60;
    export let unit = "px";
    const range = (size, startAt = 0) => [...Array(size).keys()].map(i => i + startAt);

  </script>
  
  <style lang="scss">
    @import '../scss/base';

    .wrapper-loader {
      height: 100%;
      width: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      position: absolute;
      top: 0;
      left: 0;
      z-index: 1100;
      border-radius: 10px;
      background-color: adjust-color($color: $color-white, $alpha: -0.5) ;
    }
  
    .dot {
      height: var(--dotSize);
      width: var(--dotSize);
      background-color: $color-mid-blue;
      margin: 2px;
      display: inline-block;
      border-radius: 100%;
      animation: sync 0.6s ease-in-out infinite alternate both running;
    }
  
    @-webkit-keyframes sync {
      33% {
        -webkit-transform: translateY(10px);
        transform: translateY(10px);
      }
      66% {
        -webkit-transform: translateY(-10px);
        transform: translateY(-10px);
      }
      100% {
        -webkit-transform: translateY(0);
        transform: translateY(0);
      }
    }
    @keyframes sync {
      33% {
        -webkit-transform: translateY(10px);
        transform: translateY(10px);
      }
      66% {
        -webkit-transform: translateY(-10px);
        transform: translateY(-10px);
      }
      100% {
        -webkit-transform: translateY(0);
        transform: translateY(0);
      }
    }
  </style>

{#if display}
<div class="wrapper-loader" style="--size:{size}{unit}">
  {#each range(3, 1) as i}
  <div
    class="dot"
    style="--dotSize:{size * 0.25}{unit}; animation-delay:  {i * 0.07}s;"
  ></div>
  {/each}
</div>
{/if}
