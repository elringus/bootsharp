cd ..
rm -rf dist
tsc --outDir dist --declaration
cp src/dotnet.d.ts dist/dotnet.d.ts
rm dist/*.g.*
rm dist/dotnet.runtime.js
rm dist/dotnet.native.js
