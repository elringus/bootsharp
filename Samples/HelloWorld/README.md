Example on consuming the library in various JavaScript environments.

- Run `dotnet publish` in "Project" folder to compile the library
- Open `browser.html` in a web browser to test global script import
- Run `npx serve` under the samples directory and navigate to `HelloWorld/browser-es.html` to test module script import
- Run `node node.js` to test CommonJS module import
- Run `node node-es.mjs` to test ES module import (requires node v17 or later)
