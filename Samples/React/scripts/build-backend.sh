printf "Building backend...\n"
cd ../backend
dotnet publish -c release

printf "Moving binaries to public...\n"
rm -rf ../public/bin
cp -a ./bin/managed/. ../public/bin/
cp ./bin/dotnet.wasm ../public/bin/dotnet.wasm

read -r -p "Press Enter key to exit..."
