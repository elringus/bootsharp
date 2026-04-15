cd test/cs
dotnet restore --no-cache --force-evaluate
dotnet publish -p BootsharpName=embedded -p BootsharpEmbedBinaries=true -c Debug
dotnet publish -p BootsharpName=sideload -p BootsharpEmbedBinaries=false -c Debug
