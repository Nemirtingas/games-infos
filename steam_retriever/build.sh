#! /bin/bash

cd "$(dirname $0)"

dotnet publish -c Release -r linux-x64 --self-contained=true
