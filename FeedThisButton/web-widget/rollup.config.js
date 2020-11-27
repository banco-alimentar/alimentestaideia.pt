import svelte from 'rollup-plugin-svelte';
import resolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';
import livereload from 'rollup-plugin-livereload';
import { terser } from 'rollup-plugin-terser';
import sveltePreprocess from 'svelte-preprocess';
import typescript from '@rollup/plugin-typescript';
import { string } from "rollup-plugin-string";
import prefixer from 'postcss-prefix-selector';
import postcss from 'postcss';
import pkg from "./package.json";
import seqPreprocessor from 'svelte-sequential-preprocessor';
import cssScoper from 'svelte-css-scoper';

const distName = `feedthisbutton-${pkg.version}`;
const production = !process.env.ROLLUP_WATCH;

function serve() {
	let server;
	
	function toExit() {
		if (server) server.kill(0);
	}

	return {
		writeBundle() {
			if (server) return;
			server = require('child_process').spawn('npm', ['run', 'start', '--', '--dev'], {
				stdio: ['ignore', 'inherit', 'inherit'],
				shell: true
			});

			process.on('SIGTERM', toExit);
			process.on('exit', toExit);
		}
	};
}

export default {
	input: 'src/main.ts',
	output: {
		sourcemap: true,
		format: 'iife',
		name: 'app',
		file: `public/${distName}.js`
	},
	plugins: [
		svelte({
			// enable run-time checks when not in production
			dev: !production,
			// we'll extract any component CSS out into
			// a separate file - better for performance
			css: css => {
				
				css.write(`${distName}.css`);
			},
			preprocess: seqPreprocessor([sveltePreprocess({ postcss: true, }), cssScoper({ staticSuffix: '-ba42'})])
		}),

		// If you have external dependencies installed from
		// npm, you'll most likely need these plugins. In
		// some cases you'll need additional configuration -
		// consult the documentation for details:
		// https://github.com/rollup/plugins/tree/master/packages/commonjs
		resolve({
			browser: true,
			dedupe: ['svelte']
		}),
		commonjs(),
		typescript({ 
			sourceMap: !production, 
			inlineSources: !production,
			rootDir: "src"  // workaround https://github.com/rollup/plugins/issues/243
		}),
		string({
			// Required to be specified
			include: "src/images/svg/*.svg",
	  
			// Undefined by default
			exclude: ["**/index.html"]
		  }),
		// In dev mode, call `npm run start` once
		// the bundle has been generated
		!production && serve(),

		// Watch the `public` directory and refresh the
		// browser on changes when not in production
		!production && livereload('public'),

		// If we're building for production (npm run build
		// instead of npm run dev), minify
		production && terser()
	],
	watch: {
		clearScreen: false
	}
};
