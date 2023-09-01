module.exports = {
    target: "webworker",
    entry: "./src/extension.ts",
    resolve: {
        extensions: [".ts", ".js"],
        mainFields: ["browser", "module", "main"]
    },
    output: {
        filename: "extension.js",
        library: { type: "commonjs" }
    },
    module: { rules: [{ test: /\.ts/, loader: "ts-loader" }] },
    externals: { vscode: "commonjs vscode" },
    performance: { hints: false }
};
