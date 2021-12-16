#!/bin/bash

Automated=false
Verbose=false
continue=true

for i in $@; do
    if [ '--automated' == "$i" ]; then
        Automated=true
    fi
    if [ '--verbose' == "$i" ]; then
        Verbose=true
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
    echo "root: $root"
fi

cd Project >$out

if $Verbose; then
    verbosity='d'
    echo "dotnet build \$sln -v \$verbosity"
else
    verbosity='q'
fi

sln="."

dotnet publish ${sln} -v ${verbosity} >$out

echo "\`dotnet publish ${sln} -v ${verbosity}\` in $(pwd) exited with $?"

popd >$out
popped=true

page="${PWD}/global.html"

if $Verbose; then
    echo "\${page}: ${page}"
fi

if (! ${Automated}); then
    if (type -f 'xdg-open'); then
        xdg-open $page >$out

        title='Confirmation'
        question='Was publishing successful?'

        echo
        echo "$title"
        echo "$question"
        select yn in "Yes" "No"; do
            case $yn in
            Yes)
                decision=true
                break
                ;;
            No)
                decision=false
                break
                ;;
            esac
        done
    else
        decision=true
    fi
else
    decision=true
fi

if $decision; then
    node common.js &>$out

    echo "\`common.js\` in $(pwd) exited with $?"

    if [ "$?" != "0" ]; then
        exit $?
    fi

    node es.mjs &>$out

    echo "\`es.mjs\` in $(pwd) exited with $?"

    if [ "$?" != "0" ]; then
        exit $?
    fi
else
    echo
    echo "---"
    echo "Cancelled by user input."
    echo
fi

if (! ${popped}); then
    popd >$out
fi

exit 0
