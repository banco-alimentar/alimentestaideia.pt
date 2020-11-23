<script lang="ts">
    export let checked:boolean = false;
    export let text:string = '';
    export let highlighError: boolean = false;
</script>

<label class="label">
    <input type="checkbox" bind:checked={checked}>
    <span class="checkbox-custom" class:error={highlighError}></span>
    <span class="text dark small normal">{text}</span>
</label>

<style lang="scss">
@import '../scss/base';

/* Styling Checkbox Starts */
.label {
    display: block;
    position: relative;
    margin: auto;
    cursor: pointer;
    clear: both;
}
.text {
    margin-left: 30px;
}
input {
    position: absolute;
    opacity: 0;
    cursor: pointer;
    user-select: none; 

}

.label .checkbox-custom {
    position: absolute;
    top: 3px;
    left: 0px;
    height: 14px;
    width: 14px;
    background-color: transparent;
    border-radius: 5px;
    transition: all 0.3s ease-out;
    border: 1px solid $color-mid-blue;
    &.error {
        border-color: $color-red;
    }

}


.label input:checked ~ .checkbox-custom {
    background-color: #FFFFFF;
    border-radius: 5px;
    transform: rotate(0deg) scale(1);
    opacity:1;
}


.label .checkbox-custom::after {
    position: absolute;
    content: "";
    left: 6px;
    top: 6px;
    height: 0px;
    width: 0px;
    border-radius: 5px;
    border: solid $color-mid-blue;
    border-width: 0 3px 3px 0;
    -webkit-transform: rotate(0deg) scale(0);
    -ms-transform: rotate(0deg) scale(0);
    transform: rotate(0deg) scale(0);
    opacity:1;
    transition: all 0.3s ease-out;
    -webkit-transition: all 0.3s ease-out;
    -moz-transition: all 0.3s ease-out;
    -ms-transition: all 0.3s ease-out;
    -o-transition: all 0.3s ease-out;
}


.label input:checked ~ .checkbox-custom::after {
  -webkit-transform: rotate(45deg) scale(1);
  -ms-transform: rotate(45deg) scale(1);
  transform: rotate(45deg) scale(1);
  opacity:1;
  left: 5px;
  top: -2px;
  width: 4px;
  height: 12px;
  border: solid $color-mid-blue;
  border-width: 0 2px 2px 0;
  background-color: transparent;
  border-radius: 0;
}



/* For Ripple Effect */
.label .checkbox-custom::before {
    position: absolute;
    content: "";
    left: 10px;
    top: 10px;
    width: 0px;
    height: 0px;
    border-radius: 5px;
    border: 2px solid #FFFFFF;
    -webkit-transform: scale(0);
    -ms-transform: scale(0);
    transform: scale(0);    
}

.label input:checked ~ .checkbox-custom::before {
    left: -3px;
    top: -3px;
    width: 14px;
    height: 14px;
    border-radius: 5px;
    -webkit-transform: scale(3);
    -ms-transform: scale(3);
    transform: scale(3);
    opacity:0;
    z-index: 999;
    transition: all 0.3s ease-out;
    -webkit-transition: all 0.3s ease-out;
    -moz-transition: all 0.3s ease-out;
    -ms-transition: all 0.3s ease-out;
    -o-transition: all 0.3s ease-out;
}

</style>