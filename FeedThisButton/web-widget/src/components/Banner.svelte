<script lang="ts">
    import Logo from '../images/svg/logo-ba.svelte';
    import RoundButton from './RoundButton.svelte';


    export let alt:boolean = false;
</script>

<div class="bar" class:alt>
    <div class="logo">
        <Logo/>
    </div>
    <div class="sub-bar">
        <div style="justify-content:bottom;" class="item">
            <p class="text light xxsmall title" class:blue={alt} >BANCO ALIMENTAR</p>
            <span class="text medium" style="display:inline" class:brown={alt}>À nossa mesa há sempre lugar para mais um. Com a sua ajuda.</span>
        </div>
        <div class="item button">
            <RoundButton alt={alt} responsive image="heart" text="Alimente esta ideia" on:click imageStyle="padding-top:0.3rem" />
        </div>
    </div>
</div>

<style lang="scss">
    @import '../scss/base';

    @media only screen and (max-width: $bp-xl) {
        .bar {
            overflow: hidden;

            .logo {
                min-width: 70px;
                margin-left:-12px !important;
                margin-top:20px !important;
                margin-bottom: -10px !important;
                margin-right: 0 !important;
                float:left;

            }
            .sub-bar {
                justify-content: space-between !important; 
                margin-right: 40px;
                width: 100%;
            }
        }

    }

    @media only screen and (min-width: $bp-lg) and (max-width: $bp-xl) {
        .bar {
            .text {
                font-size: $font-size-l;
                line-height: 1.5;
                &.title {
                    font-size: $font-size-xs;
                    line-height: 1.8;
                }
            }
        }

    }

    @media only screen and (max-width: $bp-lg) {
        .bar {
            .sub-bar {
                margin-right: 10px;
                margin-left:-5px;
            }
            .item {
                margin-left: 0 !important;
                margin-right: 0 !important;
            }
        }
    }
    @media only screen and (max-width: $bp-md) {
        .bar .text {
            &.break {
                display: block;
            }
        }
    }

    @media only screen and (max-width: $bp-sm) {
        .bar {
            .logo {
                margin-top: auto !important;
                margin-bottom: auto !important;
            }
            .sub-bar {
                margin-right: 10px;
                margin-left:0px;
            }
            .item {
                margin-left: 0 !important;
                margin-right: 0 !important;
                padding: 25px 5px 5px 5px !important;
            }

            .item.button {
                width: 100%;
                padding-bottom: 25px!important;
            }
        }
    }

    .bar {
        box-shadow: 0 2px 5px 0 rgba(0, 0, 0, 0.23);
        border-radius: 5px;
        background-color: $color-mid-blue;
        display: flex;
        flex-wrap: nowrap;
        justify-content: center;
        margin: 5px;

        .alt {
            background-color: $color-white;
        }

        p {
            margin:0 0 3px 0;
        }
        
        &.alt {
            background-color: $color-white;
            .logo {
                opacity: 0.5;
                fill: $color-light-blue-grey;
            }
        }

        .item {
            padding: 20px 0;
            margin: auto 10px 0 10px;
        }

        .logo {
           width:30px; 
           margin: auto 5px auto auto;
           opacity: 0.2;
        }
        
        .title {
            line-height: 2.25;
        }
    }

    .text.break {
            display: inline;
        }

    .sub-bar {
        display: inline-flex; 
        flex-wrap: wrap;
        justify-content: center;
        margin: auto auto auto 0px;
        align-items: baseline;
    }

    .blue {
        color: $color-mid-blue
    }

    .brown {
        color: $color-greyish-brown
    }
</style>