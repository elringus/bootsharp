#!/usr/bin/env bash
set -e

out="../.cover/"
json="../.cover/coverage.json"
url="http://localhost:3000"

trap 'kill "$serve_pid" 2>/dev/null || true; rm -rf .cover' EXIT

dotnet test Bootsharp.Common.Test/Bootsharp.Common.Test.csproj -p:CollectCoverage=true -p:ExcludeByAttribute=GeneratedCodeAttribute -p:CoverletOutput="$out"
dotnet test Bootsharp.Generate.Test/Bootsharp.Generate.Test.csproj -p:CollectCoverage=true -p:ExcludeByAttribute=GeneratedCodeAttribute -p:CoverletOutput="$out" -p:MergeWith="$json"
dotnet test Bootsharp.Inject.Test/Bootsharp.Inject.Test.csproj -p:CollectCoverage=true -p:Include='[Bootsharp.Inject]*' -p:ExcludeByAttribute=GeneratedCodeAttribute -p:CoverletOutput="$out" -p:MergeWith="$json"
dotnet test Bootsharp.Publish.Test/Bootsharp.Publish.Test.csproj -p:CollectCoverage=true -p:CoverletOutputFormat='json%2copencover' -p:ExcludeByAttribute=GeneratedCodeAttribute -p:CoverletOutput="$out" -p:MergeWith="$json"

reportgenerator '-reports:*/*.xml' '-targetdir:.cover' -reporttypes:HTML

serve .cover -l 3000 &
serve_pid=$!

until curl -fsS "$url" >/dev/null; do
    sleep 0.1
done

python -m webbrowser "$url"

wait "$serve_pid"
