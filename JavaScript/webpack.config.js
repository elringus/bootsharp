module.exports = () => ({
    resolve: { extensions: [".ts"] },
    module: { rules: [{ test: /\.ts/, loader: "ts-loader" }] },
    entry: "./src/bootsharp.ts",
    output: {
        filename: "bootsharp.js",
        library: { type: "umd", name: "bootsharp", export: "bootsharp" },
        globalObject: "this",
        clean: true
    },
    devtool: "source-map"
});
