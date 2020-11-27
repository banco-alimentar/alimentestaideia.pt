const config = {
    target: 'https://www.fidelidade.pt/',
    port: 5001,
    openUrl: 'https://www.fidelidade.pt/PT/a-fidelidade/responsabilidade-social/Responsabilidade-Social/Paginas/Banner_Test.aspx'
};
config.openUrl = config.openUrl.replace(config.target, `http://localhost:${config.port}`);

var express = require('express');
const { createProxyMiddleware } = require('http-proxy-middleware');
const open = require('open');

var app = express();

var options = {
        target: config.target, 
        changeOrigin: true
    };
var remoteSiteProxy = createProxyMiddleware(options);

app.get(/^.*(feedthisbutton-[.\d]+\.(css|js))$/, function(req, res) {
    res.download(`../web-widget/public/${req.params[0]}`);
});

app.use('/*', remoteSiteProxy);

open(config.openUrl);
app.listen(config.port);
