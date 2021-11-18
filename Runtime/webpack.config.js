module.exports = () => ({
    resolve: { extensions: [".ts"] },
    module: { rules: [{ test: /\.ts/, loader: "ts-loader" }] },
    entry: "./src/dotnet.ts",
    output: {
        filename: "dotnet.js",
        library: { type: "umd", name: "dotnet", export: "dotnet" },
        globalObject: "this",
        clean: true
    },
    devtool: "source-map"
});
