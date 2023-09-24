dotnet build Bootsharp.Generator -c Release
dotnet pack Bootsharp.Common -o .nuget -c Release
dotnet pack Bootsharp.DependencyInjection -o .nuget -c Release
dotnet pack Bootsharp -o .nuget -c Release
