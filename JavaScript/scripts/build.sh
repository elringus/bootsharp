cd ..
rm -rf dist
tsc --outDir dist --declaration
cp src/dotnet.g.d.ts dist/dotnet.g.d.ts
rm dist/*.g.js
