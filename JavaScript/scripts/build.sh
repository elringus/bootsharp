cd ..
rm -rf dist
rollup src/main.ts -o dist/bootsharp-main.js -f es -p node-resolve,typescript
rollup src/main.ts -o dist/bootsharp-main.d.ts -f es -p dts
