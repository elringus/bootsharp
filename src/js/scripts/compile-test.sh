cd test/cs
dotnet restore --no-cache --force-evaluate
dotnet publish -p BootsharpName=embedded -p BootsharpEmbedBinaries=true # -p RunAOTCompilation=true
dotnet publish -p BootsharpName=sideload -p BootsharpEmbedBinaries=false # -p RunAOTCompilation=true
