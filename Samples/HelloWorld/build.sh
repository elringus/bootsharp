#!/bin/bash

Automated=false
Verbose=false
failed=0

for i in $@; do
  if [ '--automated' == "$i" ]; then
    Automated=true
  fi
  if [ '--verbose' == "$i" ]; then
    Verbose=true
  fi
done

out="&> /dev/null"

if $Verbose; then
  out=""
fi

function Verbose {
  if $Verbose; then
    echo "$1" &> ${out}
  fi
}

function Execute {
  command=$1

  Verbose ${command}

  failedCode=$(${command})

  echo "\`${command}\` in $(pwd) exited with ${failedCode}"

  return $failedCode
}

function NpmExecute {
  fail=$1
  path=$2

  if ! $fail; then
    if [ -d ${path} ]; then
      Verbose "${path} exists."
      cd ${path} &> ${out}

      failed=$(Execute "npm install &> ${out}")

      if ! $failed; then
        failed=$(Execute "npm run build &> ${out}")
      fi

      return $failed
    fi
  fi

  return 0
}

pushd "$(pwd)" &> ${out}

root=$(pwd)

if $Verbose; then
  echo "Automated: $Automated"
  echo "Verbose: $Verbose"
  echo "root: $root"
fi

if [ -d "$(pwd)/Project" ]; then
  Verbose "$(pwd)/Project exists"
  cd Project &> ${out}

  if $Verbose; then
    verbosity='d'
  else
    verbosity='q'
  fi

  sln="."

  failed=$(Execute "dotnet publish ${sln} -v ${verbosity} &> ${out}")

  popd &> ${out}
  popped=true

  page="${PWD}/global.html"

  Verbose "\${page}: ${page}"

  if (! ${Automated}); then
    if (type -f 'xdg-open'); then
      Verbose "Launching browser..."
      xdg-open $page &> ${out}

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
    failed=$(Execute "node common.js &> ${out}")

    if ${failed}; then
      exit ${failed}
    fi

    failed=$(Execute "node es.mjs &> ${out}")

    if ${failed}; then
      exit ${failed}
    fi
  else
    echo
    echo "---"
    echo "Cancelled by user input."
    echo
  fi
fi
if (! ${popped}); then
  popd &> ${out}
fi

exit ${failed}
