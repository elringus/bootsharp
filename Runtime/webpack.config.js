module.exports = () => ({
    resolve: { extensions: [".ts"] },
    module: {
        rules: [
            { test: /\.ts/, loader: "ts-loader" },
            { test: /\.wasm/, type: "asset/inline" }
        ]
    },
    entry: "./src/dotnet.ts",
    output: {
        filename: "dotnet.js",
        library: { type: "umd", name: "dotnet", export: "dotnet" },
        globalObject: "this",
        clean: true
    },
    devtool: "source-map",
    performance: { hints: false }
});
