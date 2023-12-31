cd test/cs
dotnet publish -p:BootsharpName=embedded -p:BootsharpEmbedBinaries=true
dotnet publish -p:BootsharpName=sideload -p:BootsharpEmbedBinaries=false
