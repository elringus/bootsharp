#!/bin/bash

pushd $(pwd)

# Step 1: Determine if .Net 6.0 and pwershell are Installed
if [ ! $(type -t dotnet) ]
then
    apt=$(type -t apt);
    if [ $apt ]
    then
        echo "Installing libicu with apt."
        sudo apt install libicu-dev;
    else
        yum=$(type -t yum)
        if [ $yum ]
        then
            echo "Installing libicu with yum."
            sudo yum install libicu;

            sudo yum install libicu-devel.x86_64;

            sudo /usr/bin/pecl install intl;
        else
            zypper=$(type -t zypper)
            if [ $zypper ]
            then
                echo "Installing libicu with zypper."
                sudo zypper install libicu70;
            fi
        fi
    fi

    echo

    url="https://download.visualstudio.microsoft.com/download/pr/17b6759f-1af0-41bc-ab12-209ba0377779/e8d02195dbf1434b940e0f05ae086453/dotnet-sdk-6.0.100-linux-x64.tar.gz";
    echo "Downlading $url"
    curl -o dotnet-sdk-6.0.100-linux-x64.tar.gz --verbose $url;
    echo "Extracting to $HOME/dotnet"
    mkdir -p $HOME/dotnet && tar zxf dotnet-sdk-6.0.100-linux-x64.tar.gz -C $HOME/dotnet;

    echo "Exporting variables."
    export DOTNET_ROOT=$HOME/dotnet;
    export PATH=$PATH:$HOME/dotnet:$HOME/.dotnet/tools;

    dotnet=$(type -t dotnet);
    echo "dotnet: $dotnet"

    echo "Updating ~/.profile"
    echo "export DOTNET_ROOT=\$HOME/dotnet" >> ~/.profile;
    echo "export PATH=\$PATH:\$HOME/dotnet:\$HOME/.dotnet/tools" >> ~/.profile;
else
    echo "dotnet found to be type: $(type -t dotnet)"
    which dotnet
fi

# Step 2: Install/Update Powershell
if [ $(type -t dotnet) ]
then
    echo "Installing or Updating PowerShell as dotnet tool."
    # Install Powershell
    dotnet tool update powershell -g

    pwsh --version
else
    echo "Dotnet not found and unable to install it."
    exit 1
fi

# Step 3: Build with Powershell
if [ $(type -t pwsh) ]
then
    pwsh ./build-all.ps1 $1 $2 $3 $4 $5
else
    echo "Powershell still not found.  Aborting."
fi