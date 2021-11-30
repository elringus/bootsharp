[![npm](https://img.shields.io/npm/v/dotnet-js-interop)](https://www.npmjs.com/package/dotnet-js-interop)

## What is this?

A clone of [@microsoft/dotnet-js-interop](https://www.npmjs.com/package/@microsoft/dotnet-js-interop) repacked as [UMD](https://github.com/umdjs/umd) library to be used in any environment (browser, node.js, webview, etc). The library is used as a component for interop between .NET and JavaScript and is not a standalone runtime.

In case you're looking for a .NET runtime compiled to a single-file UMD library, check out [dotnet-runtime](https://www.npmjs.com/package/dotnet-runtime).

## Why?

Microsoft is not planning to support any environment, except browser: https://github.com/dotnet/aspnetcore/issues/38208

## How to use?

API and behaviour is exactly the same as in the original library: https://docs.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/call-dotnet-from-javascript

Import depends on the environment:

### CommonJS

```js
const DotNet = require("dotnet-js-interop");
// ...
DotNet.invokeMethodAsync("Assembly", "Method", args);
```

### AMD

```js
require(["DotNet"], function (DotNet) {
    DotNet.invokeMethodAsync("Assembly", "Method", args);
});
```

### Script tag

```html
<!DOCTYPE html>
<html>
    ...
    <script src="dotnet-js-interop.js"></script>
    <script>
        DotNet.invokeMethodAsync("Assembly", "Method", args);
    </script>
</html>
```
