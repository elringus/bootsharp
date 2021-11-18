module.exports = {
    target: "webworker",
    entry: "./src/extension.js",
    resolve: {
        extensions: [".js"],
        mainFields: ["browser", "module", "main"]
    },
    externals: { vscode: "commonjs vscode" },
    output: {
        filename: "extension.js",
        library: { type: "commonjs" }
    },
    performance: { hints: false }
};
