<script lang="ts">
	import Banner from './components/Banner.svelte';
	import Modal from './components/Modal.svelte';
	import { executionMode } from './store/Global';
	import type { ExecutionMode }  from './store/Global';

	export let alt:boolean = false;
	export let execMode:ExecutionMode = 'mock';
	if(execMode) $executionMode = execMode;

	let closed:boolean = true;
</script>
<!-- ba42 is a prefixed class for global css -->
<div class="ba42 text">
	<div style="width:100%;">
		<Banner on:click={ () => closed = false } bind:alt/>
	</div>
	<Modal bind:closed />

</div>

<style type="text/scss" global>
	@import './scss/base';

	@mixin font-face($name, $path, $weight: null, $style: null) {
		@font-face {
			font-family: quote($name);
			font-style: $style;
			font-weight: $weight;
			src: url(quote($path + ".ttf")) format("truetype");
			@content;
		}
	}


	$font-file-path: 'fonts/';
	@include font-face('Spartan', $font-file-path + 'Spartan-Light', $weight: 300);
	@include font-face('Spartan', $font-file-path + 'Spartan-Regular');
	@include font-face('Spartan-SemiBold', $font-file-path + 'Spartan-Semibold', $weight: 600);
	@include font-face('Spartan-Bold', $font-file-path + 'Spartan-Bold', $weight: 800);

	.ba42 {
		// global resets because of page conflicts
		*:after, *:before, * {
			box-sizing: content-box;
		}

		label {
			margin:0;
		}
	}

	.text {
		font-family: 'Spartan', sans-serif;
		font-size: $font-size-xs;
		font-weight: 600;
		font-stretch: normal;
		font-style: normal;
		line-height: 1.5em;
		letter-spacing: normal;
		color: $color-white;
		text-align: left;

		&.sp225 {
			letter-spacing: 2.25px;
		}

		&.dark {
			color: $color-greyish-brown;
		}
		&.error {
			color: $color-red;
		}
		&.alt {
			color: $color-mid-blue;
		}

		&.light {
			font-weight: 300;
		}
		&.normal {
			font-weight: normal;
		}
		&.semibold {
			font-weight: 600;
		}
		&.bold {
			font-weight: bold;
		}

		&.xxsmall {
			font-size: $font-size-xxs;
		}

		&.xsmall {
			font-size: $font-size-xs;
		}
		&.small {
			font-size: $font-size-s;
		}
		&.medium {
			font-size: $font-size-m;
		}
		&.mediumlg {
			font-size: $font-size-ml;
		}
		&.large {
			font-size: $font-size-l;
		}
		&.center {
			text-align: center;
		}
	}
</style>
