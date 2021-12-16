#!/bin/bash

Automated=false
Verbose=false
Clean=false
NoBuild=false
Publish=false
continue=true

for i in $@; do
    if [ '--automated' == "$i" ]; then
        Automated=true
    fi
    if [ '--verbose' == "$i" ]; then
        Verbose=true
    fi
    if [ '--clean' == "$i" ]; then
        Clean=true
    fi
    if [ '--no-build' == "$i" ]; then
        NoBuild=true
    fi
    if [ '--publish' == "$i" ]; then
        Publish=true
    fi
done

out="/dev/null"

if $Verbose; then
    out="/dev/tty"
fi

pushd $(pwd) >$out

root=$(pwd)

if $Verbose; then
    echo "Automated: $Automated"
    echo "Verbose: $Verbose"
    echo "Clean: $Clean"
    echo "NoBuild: $NoBuild"
    echo "Publish: $Publish"
    echo "root: $root"
fi

if $Clean; then
    if $Verbose; then
        echo "Removing node_modules, obj, bin and dist folders."
    fi
    find ${PWD} -type d -name 'node_modules' -exec rm -rf '{}' \;  >$out
    find ${PWD} -type d -regextype posix-extended -regex '.*/(bin|obj|dist)' -exec rm -rf '{}' \;  >$out
fi

if (! $NoBuild); then
    cd $root >$out

    if [ -d "$root/DotNet/" ]; then
        cd "$root/DotNet/" >$out

        sln="./DotNetJS.sln"

        if [ -f $sln ]; then
            if $Verbose; then
                verbosity='d'
                echo "dotnet build \$sln -v \$verbosity"
            else
                verbosity='q'
            fi

            dotnet build ${sln} -v ${verbosity} >$out

            echo "\`dotnet build ${sln} -v ${verbosity}\` in $(pwd) exited with $?"

            failedCode="$?"
            failedStep="$(pwd): dotnet build ${sln} -v ${verbosity}"
            if [ "$failedCode" != "0" ]; then
                continue=false
            fi
        fi
    fi

    # Build JavaScript JS Interop
    if $continue; then
        if [ -d "$root/JavaScript/dotnet-js-interop/" ]; then
            cd "$root/JavaScript/dotnet-js-interop/" >$out

            if $Verbose; then
                echo "npm install"
            fi
            npm install >$out
            echo "\`npm install\` in $(pwd) exited with $?"

            failedCode="$?"
            failedStep="$(pwd): npm install"
            if [ "$failedCode" != "0" ]; then
                continue=false
            fi

            if $continue; then
                if $Verbose; then
                    echo "npm run build"
                fi
                npm run build >$out
                echo "\`npm run build\` in $(pwd) exited with $?"

                failedCode="$?"
                failedStep="$(pwd): npm run build"
                if [ "$failedCode" != "0" ]; then
                    continue=false
                fi
            fi
        fi
    fi

    # Build JavaScript dotnet-runtime
    if $continue; then
        if [ -d "$root/JavaScript/dotnet-runtime/" ]; then
            cd "$root/JavaScript/dotnet-runtime/" >$out

            if $Verbose; then
                echo "npm install"
            fi
            npm install >$out
            echo "\`npm install\` in $(pwd) exited with $?"

            failedCode="$?"
            failedStep="$(pwd): npm install"
            if [ "$failedCode" != "0" ]; then
                continue=false
            fi

            if $continue; then
                if $Verbose; then
                    echo "npm run build"
                fi
                npm run build >$out
                echo "\`npm run build\` in $(pwd) exited with $?"

                failedCode="$?"
                failedStep="$(pwd): npm run build"
                if [ "$failedCode" != "0" ]; then
                    continue=false
                fi
            fi
        fi
    fi

    # Build Samples
    if $continue; then
        if [ -d "$root/Samples/HelloWorld/" ]; then
            cd "$root/Samples/HelloWorld/" >$out

            if $Automated; then
                automate="--automated"
            fi

            if $Verbose; then
                vrbse="--verbose"
                echo ". ./build.sh $automate $vrbse"
            fi

            /bin/bash $(pwd)/build.sh ${automate} ${vrbse}

            failedCode="$?"
            failedStep="${next}: . ./build.sh ${automate} ${vrbse}"
            if [ "$failedCode" != "0" ]; then
                continue=false
            fi
        fi
    fi

    # Build Extension
    if $continue; then
        if [ -d "$root/Samples/WebExtension/" ]; then
            cd "$root/Samples/WebExtension/" >$out

            if $Verbose; then
                echo "npm install"
            fi
            npm install >$out
            echo "\`npm install\` in $(pwd) exited with $?"

            failedCode="$?"
            failedStep="$(pwd): npm install"
            if [ "$failedCode" != "0" ]; then
                continue=false
            fi

            if $continue; then
                if $Verbose; then
                    echo "npm run build"
                fi
                npm run build >$out
                echo "\`npm run build\` in $(pwd) exited with $?"

                failedCode="$?"
                failedStep="$(pwd): npm run build"
                if [ "$failedCode" != "0" ]; then
                    continue=false
                fi
            fi
        fi
    fi

    if ! $continue; then
        echo "Exit code ${failedCode} at step: $failedStep"
    fi

    echo
    echo "---"
    echo "Successfully built all portions of the repository!"
    echo
fi

if $Publish; then
    echo "Steps to publish the components go here."
fi

popd >$out
