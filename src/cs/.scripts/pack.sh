#!/usr/bin/env bash
set -e

if [ ! -f .llvm/microsoft.dotnet.ilcompiler.llvm/build/Microsoft.DotNet.ILCompiler.LLVM.targets ]; then
  echo "NativeAOT-LLVM artifacts are not downloaded. Run ./.scripts/llvm.sh." >&2
  exit 1
fi

mkdir -p .nuget
dotnet build Bootsharp.Generate -c Release
dotnet pack Bootsharp.Common -o .nuget -c Release
dotnet pack Bootsharp.Inject -o .nuget -c Release
dotnet pack Bootsharp -o .nuget -c Release
dotnet restore
