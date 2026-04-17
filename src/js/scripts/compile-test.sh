cd test/cs
rm -rf Test/bin Test/obj Test.Types/bin Test.Types/obj
dotnet restore --no-cache --force-evaluate
dotnet publish -p BootsharpName=embedded -p BootsharpEmbedBinaries=true -c Debug
rm -rf Test/bin/Debug Test/obj Test.Types/bin Test.Types/obj
dotnet publish -p BootsharpName=sideload -p BootsharpEmbedBinaries=false -c Debug
