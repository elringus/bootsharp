# dotnet workload install wasm-tools --source https://api.nuget.org/v3/index.json
cd ../..
dotnet build Packer/Packer.csproj
dotnet build Generator/Generator.csproj
dotnet build DotNetJS/DotNetJS.csproj
dotnet publish Runtime/test/project/Test.csproj #-fl -flp:logfile=Build.log;verbosity=diagnostic
read -r -p "Press Enter key to exit..."
