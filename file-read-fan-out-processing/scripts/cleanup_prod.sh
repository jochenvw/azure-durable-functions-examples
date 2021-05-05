#!/bin/sh

# Cleans up DEV resources in resource group
echo "NOTE: this will delete the PROD resource group"
az group delete --name ms-csu-nl-jvw-filetrnsfrm-prod