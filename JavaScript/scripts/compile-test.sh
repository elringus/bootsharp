# dotnet workload install wasm-tools --source https://api.nuget.org/v3/index.json
# dotnet build-server shutdown
cd ../tests/cs
dotnet workload restore
dotnet publish #-v:Normal -fl -flp:logfile=Build.log;verbosity=diagnostic
