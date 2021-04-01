#!/bin/bash

ENV=$1

#Deploy .NET 5 Server-Gigya
PKG_VERSION=$(xmllint --xpath "string(//Project/PropertyGroup/AssemblyVersion)" ./OwnID.Server.Gigya/OwnID.Server.Gigya.csproj)
IMAGE_URI=$DOCKER_URL/$ENV/single-server/ownid-server-gigya:${PKG_VERSION-}

echo Docker push to $IMAGE_URI
docker tag ownid-server-gigya:latest $IMAGE_URI
docker push $IMAGE_URI

echo K8S cluster selection
aws eks --region us-east-2 update-kubeconfig --name ownid-eks

echo Update IMAGE in base kustomization.yaml
(cd manifests/base && kustomize edit set image server-gigya=$IMAGE_URI)

echo Applications update

if [ "$ENV" == "dev" ]; then
  apps=(demo-gigya)
else
  apps=(demo-gigya dor)
fi


for app in "${apps[@]}"; do
  echo Deploying $app
  kustomize build manifests/$ENV/$app/ | kubectl apply -f -
  echo
done