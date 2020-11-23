import App from './App.svelte';

let root = document.getElementById('ba-feedthisbutton-root');
if(!root) {
	console.log('feedthisbutton: widget root not found!');
}

let cssLink = document.createElement('link');
cssLink.rel = 'stylesheet';
cssLink.href = getCssPath();
cssLink.onload = () => {
	const app = new App({
		target: root,
		props: {
			alt: root.getAttribute('data-alt') ? true : false,
			execMode: root.getAttribute('data-mode'),
		}
	});
};
document.head.appendChild(cssLink);


function getCssPath(): string {
	const feedthisbuttonScript = /feedthisbutton[.\d-]+\.js$/;
	let scripts = document.body.getElementsByTagName('script');
	for(let script of scripts) {
		if(feedthisbuttonScript.test(script.src)) {
			return script.src.replace(/\.js$/, '.css');
		}
	}
	console.log('feedthisbutton: could not load css');
	return '';
}