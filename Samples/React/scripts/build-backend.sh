printf "Building backend...\n\n"
cd ../backend
dotnet publish -c release

printf "\nCopying build artifacts to public...\n\n"
rm -rf ../public/bin
cp -a ./Backend.WASM/bin/managed/. ../public/bin/
cp ./Backend.WASM/bin/dotnet.wasm ../public/bin/dotnet.wasm

read -r -p "Build complete. Press Enter key to exit..."
