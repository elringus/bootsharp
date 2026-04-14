#!/usr/bin/env bash
set -e

# Find the latest available version with:
# nuget search "Microsoft.DotNet.ILCompiler.LLVM" -source "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json" -prerelease -take 1

VERSION="10.0.0-rc.1.26117.1"
FEED="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/flat2"
PACKAGES=(
  "microsoft.dotnet.ilcompiler.llvm"
  "runtime.browser-wasm.microsoft.dotnet.ilcompiler.llvm"
  "runtime.win-x64.microsoft.dotnet.ilcompiler.llvm"
  "runtime.linux-x64.microsoft.dotnet.ilcompiler.llvm"
)

rm -rf .llvm
mkdir -p .llvm

for package in "${PACKAGES[@]}"; do
  curl -fsSL "$FEED/$package/$VERSION/$package.$VERSION.nupkg" -o .llvm/$package.nupkg
  mkdir -p .llvm/$package
  unzip -q .llvm/$package.nupkg -d .llvm/$package
  rm -rf .llvm/$package/_rels
  rm .llvm/$package.nupkg
done
