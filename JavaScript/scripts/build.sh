cd ..
rm -rf dist
rollup src/main.ts -o dist/bootsharp-main.js -f es -p node-resolve,typescript \
-e "./dotnet.runtime.js","./dotnet.native.js","./dotnet.js","./bootsharp-resources.js","./bootsharp-bindings.js"
rollup src/main.ts -o dist/bootsharp-main.d.ts -f es -p dts -e "./bootsharp-bindings.js"
sed -i "1,4d" dist/bootsharp-main.d.ts
