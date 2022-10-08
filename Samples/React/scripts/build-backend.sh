printf "Building backend...\n\n"
cd ../backend
dotnet publish -c release

printf "\nCopying build artifacts to public...\n\n"
rm -rf ../public/bin
cp -a ./bin/managed/. ../public/bin/
cp ./bin/dotnet.wasm ../public/bin/dotnet.wasm
cp ./bin/dotnet-worker.js ../public/bin/dotnet-worker.js

read -r -p "Build complete. Press Enter key to exit..."
