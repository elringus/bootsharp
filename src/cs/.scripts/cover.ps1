try {
    $out = "../.cover/"
    $json = "../.cover/coverage.json"
    dotnet test Bootsharp.Common.Test/Bootsharp.Common.Test.csproj /p:CollectCoverage=true /p:ExcludeByAttribute=GeneratedCodeAttribute /p:CoverletOutput=$out
    dotnet test Bootsharp.Generate.Test/Bootsharp.Generate.Test.csproj /p:CollectCoverage=true /p:ExcludeByAttribute=GeneratedCodeAttribute /p:CoverletOutput=$out /p:MergeWith=$json
    dotnet test Bootsharp.Inject.Test/Bootsharp.Inject.Test.csproj /p:CollectCoverage=true /p:ExcludeByAttribute=GeneratedCodeAttribute /p:CoverletOutput=$out /p:MergeWith=$json
    dotnet test Bootsharp.Publish.Test/Bootsharp.Publish.Test.csproj /p:CollectCoverage=true /p:CoverletOutputFormat="json%2copencover" /p:ExcludeByAttribute=GeneratedCodeAttribute /p:CoverletOutput=$out /p:MergeWith=$json
    reportgenerator "-reports:*/*.xml" "-targetdir:.cover" -reporttypes:HTML
    python -m webbrowser http://localhost:3000
    npx serve .cover
} finally {
    rm .cover -r -force
}
