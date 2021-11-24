# dotnet workload install wasm-tools --source https://api.nuget.org/v3/index.json
cd ../..
dotnet restore Packer/Packer.csproj
dotnet publish DotNetJS/DotNetJS.csproj -c Release
dotnet publish Runtime/test/project/Test.csproj -c Release #-fl -flp:logfile=Build.log;verbosity=diagnostic
read -r -p "Press Enter key to exit..."
