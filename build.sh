#!/bin/bash

Automated=false
Verbose=false
Clean=false
NoBuild=false
Publish=false
failed=0

# shellcheck disable=SC2068
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

out='/dev/null'

if $Verbose; then
  out="/dev/tty"
fi

function Verbose {
  if $Verbose; then
    echo "$1" &> ${out}
  fi
}

function Execute {
  command=$1

  Verbose ${command}

  result=$(${command})
  failedCode=$?
  echo "48: (${failedCode}) :: while executing [${command}] :: \${result}: [${result}]" &> /dev/tty
  
  if [ "${failedCode}" != "0" ] ; then
    "51: Failure (${failedCode}) executing command: [${command}]"
    return ${failedCode}
  fi

  echo "\`${command}\` in $(pwd) exited successfully." &> /dev/tty

  "57: (${failedCode}) [${command}]"
  return $failedCode
}

function NpmExecute {
  path=$2
  
  if [ ! -d ${path} ]; then
    "71: Failed to execute NPM because \${path}: ${path} is not found."
    return 126;
  fi

  if [ -d ${path} ]; then
    Verbose "${path} exists."
    cd ${path} &> ${out}

    result=$(Execute "npm install &> ${out}")
    fail=$?
    echo "81: (${fail}): [${result}]" &> ${out}

    if [ "${fail}" != "0" ] ; then
      "84: Failure (${fail}) executing command: [npm install] :: \${result}: [${result}]"
      return ${fail}
    fi
  fi

  "89: (${fail}) :: [${result}]"
  return ${fail}
}

function ExitIfFailed {
  fail=$1
  message=$2
  
  if [ "${fail}" != "0" ] ; then
    echo "FAILED: (${fail}): ${message}" > /dev/tty
    exit ${fail}
  fi
}

echo "\${out}: ${out}"

# shellcheck disable=SC2046
pushd $(pwd) &> ${out}

root=$(pwd)

if $Verbose; then
  echo "Automated: $Automated"
  echo "Verbose: $Verbose"
  echo "Clean: $Clean"
  echo "NoBuild: $NoBuild"
  echo "Publish: $Publish"
  echo "root: ${root}"
fi

if $Clean; then
  Verbose "Removing node_modules, obj, bin and dist folders."
  find "${PWD}" -type d -name 'node_modules' -exec rm -rf '{}' \; &> ${out}
  find "${PWD}" -type d -regextype posix-extended -regex '.*/(bin|obj|dist)' -exec rm -rf '{}' \; &> ${out}
else
  Verbose "Clean not requested (\$Clean: $Clean)"
fi

if (! $NoBuild); then
  Verbose "Building..."

  cd ${root} &> ${out}

  if [ -d "${root}/DotNet/" ]; then
    Verbose "${root}/DotNet/ exists."

    cd "${root}/DotNet/" &> ${out}

    sln="./DotNetJS.sln"

    if [ -f $sln ]; then
      Verbose "${sln} exists."

      if $Verbose; then
        verbosity='d'
      else
        verbosity='q'
      fi

      result=$(Execute "dotnet build ${sln} -v ${verbosity} &> ${out}")
      failed=$?
      ExitIfFailed ${failed} ${result}
    fi
  fi

  # Build JavaScript JS Interop
  result="[LINE 155] $(NpmExecute $failed "${root}/JavaScript/dotnet-js-interop/")"
  failed=$?
  ExitIfFailed ${failed} ${result}

  # Build JavaScript dotnet-runtime
  result="[LINE 160] $(NpmExecute $failed "${root}/JavaScript/dotnet-runtime/")"
  failed=$?
  ExitIfFailed ${failed} ${result}

  # Build Samples
  if [ -d "${root}/Samples/HelloWorld/" ]; then
    Verbose "${root}/Samples/HelloWorld/ exists."
    cd "${root}/Samples/HelloWorld/" &> ${out}

    if $Automated; then
      automate="--automated"
    fi

    if $Verbose; then
      vrbse="--verbose"
    fi

    result="[LINE 177] $(Execute "/bin/bash \"$(pwd)/build.sh\" ${automate} ${vrbse}")"
    failed=$?
    ExitIfFailed ${failed} ${result}
  fi

  # Build Extension
  result="[LINE 183] $(NpmExecute $failed "${root}/Samples/WebExtension/")"
  failed=$?
  ExitIfFailed ${failed} ${result}

  echo
  echo "---"
  echo "Successfully built all portions of the repository!"
  echo
else
  Verbose "No-Build requested  (\$NoBuild: $NoBuild)"
fi

if $Publish; then
  echo "Steps to publish the components go here."
else
  Verbose "Publish not requested (\$Publish: $Publish)"
fi

popd &> ${out}
exit ${failedCode}
