const CopyPlugin = require("copy-webpack-plugin");
const vendorPath = "./node_modules/@microsoft/dotnet-js-interop/dist/Microsoft.JSInterop.";

module.exports = () => ({
    entry: "./src/main.js",
    plugins: [
        new CopyPlugin({
            patterns: [
                { from: vendorPath + "d.ts", force: true },
                { from: vendorPath + "js.map", force: true }
            ]
        })
    ],
    output: {
        filename: "Microsoft.JSInterop.js",
        library: { type: "umd" },
        globalObject: "this",
        clean: true
    }
});
