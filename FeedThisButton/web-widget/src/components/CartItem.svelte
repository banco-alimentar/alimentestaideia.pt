<script lang="ts">
    import imgs from '../images';
    import { cart } from '../store/Cart';

    export let image :string;
    export let item :string = "";

    let imageComponent = imgs[image];

</script>

<div class="item text semibold medium dark">
    <div class="image-row">
        <div class="image">
            <svelte:component this={ imageComponent }/>
        </div>
    </div>
    <div class="label">{$cart.getLabel(item)}</div>
    <div class="buttons">
        <div class="circle text large minus center" on:click={() => cart.removeItem(item)}>&#8211;</div>
        <div class="counter">{$cart.getQuantity(item)}</div>
        <div class="circle text large plus center" on:click={() => cart.addItem(item)}>&#43;</div>
    </div>
</div>

<style lang="scss">
    @import '../scss/base';

    $color-background: #fffeff;
    @media only screen and (max-width: $bp-sm) {
        .item.text {
            width: 100%;
            height: auto;
            margin: 15px 0;
            & > div {
                margin: 20px 5px;
            }
            max-width: unset;
            padding: 0 20px;
            .image-row {
                margin-left: 5px;
                padding-top: 0;
                width: 10%;
                .image {
                    width: 100%;
                }
            }
            .label {
                width: auto;
                text-align: left;
            }
            .buttons {
                width: auto;
                margin-left:auto;
            }
        }
    }
    .label {
        margin-bottom: 10px;
    }
    .counter {
        color: $color-mid-blue;
    }
    .item {
        width: calc(100% / 2 - 10px);
        margin: 10px 0;
        padding: 25px 0 15px 0;
        border-radius: 10px;
        box-shadow: 3px 1px 13px 4px rgba(0, 0, 0, 0.07);
        background-color: #fffeff;
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        & > div {
            width: 100%;
            text-align: center;
        }

        .image-row{
            position: relative;
        }

        .image {
            margin-left: auto; // minor ajusment because of the shadow
            margin-right: auto;
            bottom: 0px;
            width: 20px;
            svg  {
                display: inline-block;
            }
        }

        .buttons {
            display: flex;
            justify-content: center;
        }
        .counter {
            width: 31px;
        }
        .circle {
            width: 22px;
            height: 22px;
            border-radius: 25px;
            background-color: $color-mid-blue;
            color: $color-white;
            &:hover {
                cursor: pointer;
                background-color: adjust-color($color: $color-mid-blue, $alpha: -0.2);
            }
            &::selection {
                background-color: transparent;
            }
        }
        .minus {
            line-height: 1.1em;
        }
        .plus {
            line-height: 1.3em;
        }
    }
</style>