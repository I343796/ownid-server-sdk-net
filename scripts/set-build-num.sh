#!bin/bash

BUILD_NUM=$1

echo 'Adding build number '$BUILD_NUM' to version'
sed -i -r -e 's/(<AssemblyVersion>[0-9]{1,2}\.[0-9]{1,3}\.)[0-9]*([A-Za-z0-9\-]*<\/AssemblyVersion>)/\1'$BUILD_NUM'\2/gm' ./OwnID.Server/OwnID.Server.csproj
sed -i -r -e 's/(<AssemblyVersion>[0-9]{1,2}\.[0-9]{1,3}\.)[0-9]*([A-Za-z0-9\-]*<\/AssemblyVersion>)/\1'$BUILD_NUM'\2/gm' ./OwnID.Server.WebApp/OwnID.Server.WebApp.csproj