rm -rf dist
tsc --outDir dist --declaration
mkdir -p dist/dotnet
cp src/dotnet/*.d.ts dist/dotnet/
