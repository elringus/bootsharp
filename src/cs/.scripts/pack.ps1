dotnet build Bootsharp.Generate -c Release
dotnet pack Bootsharp.Common -o .nuget -c Release
dotnet pack Bootsharp.Inject -o .nuget -c Release
dotnet pack Bootsharp -o .nuget -c Release
dotnet restore
