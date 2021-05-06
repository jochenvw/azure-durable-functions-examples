# Infrastructure

Infrastucture for these examples is deployed using [Bicep](https://github.com/Azure/bicep). 
After installing the Bicep tooling - deploy the environment by executing the commands in `deploy.azcli` or:

```bash
az group create --name durable-function-examples --location westeurope 
az deployment group create -f ./main.bicep -g durable-function-examples
```