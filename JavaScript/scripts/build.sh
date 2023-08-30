cd ..
rm -rf dist
tsc --outDir dist --declaration
cp src/dotnet.d.ts dist/dotnet.d.ts
