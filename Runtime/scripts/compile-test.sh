# dotnet workload install wasm-tools --source https://api.nuget.org/v3/index.json
cd ../test/project
rm -rf bin
rm -rf obj
dotnet publish -c Release -fl -flp:logfile=Build.log;verbosity=diagnostic
read -r -p "Press Enter key to exit..."
