param (
    [switch]
    $Automated = $false,
    [switch]
    $Verbose = $false,
    [switch]
    $Clean = $false,
    [switch]
    $NoBuild = $false,
    [switch]
    $Publish = $false
)

Push-Location -Verbose:$Verbose
$root = Get-Location -Verbose:$Verbose

try {
    if ($Clean) {
        Get-ChildItem obj, bin, dist, node_modules -Recurse -ErrorAction SilentlyContinue -Verbose:$Verbose `
        | Remove-Item -Force -Recurse -ErrorAction Stop -Verbose:$Verbose
    }

    if (-not $NoBuild) {
        Set-Location $root/Dotnet -Verbose:$Verbose
        $sln = Get-Item ./DotNetJS.sln -Verbose:$Verbose -ErrorAction SilentlyContinue

        $continue = (Test-Path $sln -ErrorAction SilentlyContinue -Verbose:$Verbose) ?? $false

        # Build DotNetJS Solution
        if ($continue) {
            $verbosity = $Verbose ? 'd' : 'n';

            Write-Verbose "& dotnet build `$sln.FullName -v `$verbosity" -Verbose:$Verbose
            & dotnet build $sln.FullName -v $verbosity

            $continue = $LASTEXITCODE -eq 0
        }

        # Build JavaScript JS Interop
        if ($continue) {
            $path = Resolve-Path $root/JavaScript/dotnet-js-interop -Verbose:$Verbose
            if (Test-Path $path -Verbose:$Verbose) {
                Set-Location $path -Verbose:$Verbose

                Write-Verbose "& npm install" -Verbose:$Verbose
                & npm install
                $continue = $LASTEXITCODE -eq 0

                if($continue) {
                    Write-Verbose "& npm run build" -Verbose:$Verbose
                    & npm run build

                    $continue = $LASTEXITCODE -eq 0
                }
            }
            else {
                throw "Could not locate JavaScript path at $path.";
            }
        }

        # Build JavaScript dotnet-runtime
        if ($continue) {
            $path = Resolve-Path $root/JavaScript/dotnet-runtime -Verbose:$Verbose
            if (Test-Path $path -Verbose:$Verbose) {
                Set-Location $path -Verbose:$Verbose

                Write-Verbose "& npm install" -Verbose:$Verbose
                & npm install
                $continue = $LASTEXITCODE -eq 0

                if($continue) {
                    Write-Verbose "& npm run build" -Verbose:$Verbose
                    & npm run build

                    $continue = $LASTEXITCODE -eq 0
                }
            }
            else {
                throw "Could not locate JavaScript path at $path.";
            }
        }

        # Build Samples
        if ($continue) {
            $path = Resolve-Path $root/Samples/HelloWorld -Verbose:$Verbose
            if (Test-Path $path -Verbose:$Verbose) {
                Set-Location $path -Verbose:$Verbose

                Write-Verbose ". ./build.ps1 -Automated:`$Automated -Verbose:`$Verbose" -Verbose:$Verbose
                . ./build.ps1 -Automated:$Automated -Verbose:$Verbose

                $continue = $LASTEXITCODE -eq 0
            }
            else {
                throw "Could not locate Sample path at $path.";
            }
        }

        # Build Extension
        if ($continue) {
            $path = Resolve-Path $root/Samples/WebExtension -Verbose:$Verbose
            if (Test-Path $path -Verbose:$Verbose) {
                Set-Location $path -Verbose:$Verbose

                Write-Verbose "& npm install" -Verbose:$Verbose
                & npm install
                $continue = $LASTEXITCODE -eq 0

                if($continue) {
                    Write-Verbose "& npm run build" -Verbose:$Verbose
                    & npm run build

                    $continue = $LASTEXITCODE -eq 0
                }
            }
            else {
                throw "Could not locate Sample path at $path.";
            }
        }
    }

    if($Publish) {
        # Steps to publish the components go here.
    }
}
finally {
    Pop-Location -Verbose:$Verbose
}
