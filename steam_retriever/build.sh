#! /bin/bash

cd "$(dirname $0)"

export DOTNET_CLI_TELEMETRY_OPTOUT=1

dotnet publish -c Release -r linux-x64 --self-contained=true
