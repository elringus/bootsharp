# dotnet workload install wasm-tools --source https://api.nuget.org/v3/index.json
dotnet build-server shutdown
dotnet build ../../DotNet/DotNetJS.sln
dotnet publish ../test/csharp/Test.sln #-v:Normal -fl -flp:logfile=Build.log;verbosity=diagnostic
