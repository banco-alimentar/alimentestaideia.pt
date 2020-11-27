# Feed this Button
Built using [Svelte.js](https://svelte.dev/)

## Build commands
##### should be executed in the web-widget dir
```shell
# install dependencies:
npm install

# build and launch development web server
npm run dev

# build for production
npm run build
```



## Testing inside a partner website
This is usefull to debug issues with the website global css
It proxies the partner website, replacing the feedthisbutton javascript and css files for the ones in our computer
##### should be executed in the test-proxy dir

```shell
# start web-proxy
npm run proxy
```
