cd ..
rm -rf dist
tsc --outDir dist --declaration
cp src/dotnet.d.ts dist/dotnet.d.ts
rm dist/*.g.*
rm dist/dotnet.runtime*
rm dist/dotnet.native*
