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

Write-Verbose "`$root: $root" -Verbose:$Verbose;

# if($Verbose) {
#     Set-Location $root -Verbose:$Verbose -ErrorAction Stop
#     Get-ChildItem * -Verbose:$Verbose -ErrorAction Stop -Recurse `
#     | format-Table -Property UnixMode, Size, FullName 
# }

try {
    if ($Clean) {
        Get-ChildItem obj, bin, dist, node_modules -Recurse -ErrorAction SilentlyContinue -Verbose:$Verbose `
        | Remove-Item -Force -Recurse -ErrorAction Stop -Verbose:$Verbose
    }

    if (-not $NoBuild) {
        Set-Location $root -Verbose:$Verbose -ErrorAction Stop
        $next = Resolve-Path $root/DotNet/ -Verbose:$Verbose -ErrorAction Stop

        Set-Location ${next} -Verbose:$Verbose -ErrorAction Stop
        $sln = Get-Item ./DotNetJS.sln -Verbose:$Verbose -ErrorAction SilentlyContinue

        $continue = (Test-Path $sln -ErrorAction SilentlyContinue -Verbose:$Verbose) ?? $false

        # Build DotNetJS Solution
        if ($continue) {
            $verbosity = $Verbose ? 'd' : 'n';

            Write-Verbose "& dotnet build `$sln.FullName -v `$verbosity" -Verbose:$Verbose
            & dotnet build $sln.FullName -v $verbosity

            $failedCode = $LASTEXITCODE
            $failedStep = "& dotnet build `$sln.FullName -v `$verbosity"
            $continue = $failedCode -eq 0
        }

        # Build JavaScript JS Interop
        if ($continue) {
            $next = Resolve-Path $root/JavaScript/dotnet-js-interop/ -Verbose:$Verbose -ErrorAction Stop
            if (Test-Path ${next} -Verbose:$Verbose) {
                Set-Location ${next} -Verbose:$Verbose

                Write-Verbose "& npm install" -Verbose:$Verbose
                & npm install

                $failedCode = $LASTEXITCODE
                $failedStep = "${next}: & npm install"
                $continue = $failedCode -eq 0

                if($continue) {
                    Write-Verbose "& npm run build" -Verbose:$Verbose
                    & npm run build

                    $failedCode = $LASTEXITCODE
                    $failedStep = "${next}: & npm run build"
                    $continue = $failedCode -eq 0
                }
            }
        }

        # Build JavaScript dotnet-runtime
        if ($continue) {
            $next = Resolve-Path $root/JavaScript/dotnet-runtime/ -Verbose:$Verbose -ErrorAction Stop
            if (Test-Path ${next} -Verbose:$Verbose) {
                Set-Location ${next} -Verbose:$Verbose

                Write-Verbose "& npm install" -Verbose:$Verbose
                & npm install

                $failedCode = $LASTEXITCODE
                $failedStep = "${next}: & npm install"
                $continue = $failedCode -eq 0

                if($continue) {
                    Write-Verbose "& npm run build" -Verbose:$Verbose
                    & npm run build

                    $failedCode = $LASTEXITCODE
                    $failedStep = "${next}: & npm run build"
                    $continue = $failedCode -eq 0
                }
            }
        }

        # Build Samples
        if ($continue) {
            $next = Resolve-Path $root/Samples/HelloWorld/ -Verbose:$Verbose -ErrorAction Stop
            if (Test-Path ${next} -Verbose:$Verbose) {
                Set-Location ${next} -Verbose:$Verbose

                Write-Verbose ". ./build.ps1 -Automated:`$Automated -Verbose:`$Verbose" -Verbose:$Verbose
                . ./build.ps1 -Automated:$Automated -Verbose:$Verbose

                $failedCode = $LASTEXITCODE
                $failedStep = "${next}: . ./build.ps1 -Automated:$Automated -Verbose:$Verbose"
                $continue = $failedCode -eq 0
            }
        }

        # Build Extension
        if ($continue) {
            $next = Resolve-Path $root/Samples/WebExtension/ -Verbose:$Verbose -ErrorAction Stop
            if (Test-Path ${next} -Verbose:$Verbose) {
                Set-Location ${next} -Verbose:$Verbose

                Write-Verbose "& npm install" -Verbose:$Verbose
                & npm install

                $failedCode = $LASTEXITCODE
                $failedStep = "${next}: & npm install"
                $continue = $failedCode -eq 0

                if($continue) {
                    Write-Verbose "& npm run build" -Verbose:$Verbose
                    & npm run build

                    $failedCode = $LASTEXITCODE
                    $failedStep = "${next}: & npm run build"
                    $continue = $failedCode -eq 0
                }
            }
        }

        if(-not continue) {
            throw "Exit code $failedCode at step:\n$failedStep"
        }
    }

    if($Publish) {
        # Steps to publish the components go here.
    }
}
finally {
    Pop-Location -Verbose:$Verbose
}
