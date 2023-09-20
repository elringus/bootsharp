# dotnet workload install wasm-tools --source https://api.nuget.org/v3/index.json
# dotnet build-server shutdown
dotnet publish ../tests/cs #-v:Normal -fl -flp:logfile=Build.log;verbosity=diagnostic
