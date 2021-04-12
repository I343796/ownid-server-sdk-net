#!/bin/bash

ENV=$1

#Deploy .NET 5 Single Server
PKG_VERSION=$(xmllint --xpath "string(//Project/PropertyGroup/AssemblyVersion)" ./OwnID.Server/OwnID.Server.csproj)
IMAGE_URI=$DOCKER_URL/$ENV/single-server/ownid-single-server:${PKG_VERSION-}

echo Docker push to $IMAGE_URI
docker tag ownid-single-server:latest $IMAGE_URI
docker push $IMAGE_URI

echo K8S cluster selection
aws eks --region us-east-2 update-kubeconfig --name ownid-eks

echo Update IMAGE in base kustomization.yaml
(cd manifests/base && kustomize edit set image single-server=$IMAGE_URI)

echo Applications update

if [ "$ENV" == "dev" ]; then
  apps=(demo-gigya demo-firebase)
else
  apps=(demo-gigya dor demo-firebase)
fi


for app in "${apps[@]}"; do
  echo Deploying $app
  kustomize build manifests/$ENV/$app/ | kubectl apply -f -
  echo
done