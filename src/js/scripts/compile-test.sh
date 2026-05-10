cd test/cs
rm -rf Test/bin Test/obj Test.Library/bin Test.Library/obj
dotnet restore --no-cache --force-evaluate
dotnet publish -c Debug
