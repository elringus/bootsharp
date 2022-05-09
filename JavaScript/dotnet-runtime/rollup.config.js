import pkg from "./package.json";
import resolve from "rollup-plugin-node-resolve";
import commonjs from "rollup-plugin-commonjs";
import dts from 'rollup-plugin-dts'
import esbuild from "rollup-plugin-esbuild";

export default [
    {
        input: pkg.source,
        output: {
            file: pkg.main,
            format: "umd",
            name: "dotnet",
        },
        external: ["dotnet-js-interop"],
        plugins: [esbuild(), commonjs(), resolve()],
    },
    {
        input: pkg.source,
        output: {
            file: pkg.types,
            format: 'esm',
        },
        plugins: [dts()],
    }
];
