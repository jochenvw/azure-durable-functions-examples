// Inspired by https://github.com/Azure/bicep/blob/main/docs/examples/101/function-app-create/main.bicep

param environmentPostfix string = 'dev'
param projectName string = 'func-sandbox'

var defaultResourceLocation = resourceGroup().location

var functionAppStoreName = replace('${projectName}-funcstore-${environmentPostfix}','-','')
var functionAppPlanName = '${projectName}-serviceplan-${environmentPostfix}'
var functionAppName = '${projectName}-app-${environmentPostfix}'
var appInsightsName = '${projectName}-telemetry-${environmentPostfix}'
var logAnalyticsName = '${projectName}-logs-${environmentPostfix}'

resource functionstore 'Microsoft.Storage/storageAccounts@2021-02-01' = {
  name: functionAppStoreName
  location: defaultResourceLocation
  sku: {
    name: 'Standard_LRS'
    tier: 'Standard'
  }
  kind: 'StorageV2'
}

resource functionappplan 'Microsoft.Web/serverfarms@2020-12-01' = {
  name: functionAppPlanName
  location: defaultResourceLocation
  kind: 'elastic'
  sku: {
    'name': 'EP2'
    'tier': 'ElasticPremium'
    'size': 'EP2'
    'family': 'EP'
    'capacity': 1
  }
}

resource logs 'Microsoft.OperationalInsights/workspaces@2020-10-01' = {
  name: logAnalyticsName
  location: defaultResourceLocation  
}

resource appInsights 'Microsoft.Insights/components@2020-02-02-preview' = {
  name: appInsightsName
  location: defaultResourceLocation
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Function App
resource functionApp 'Microsoft.Web/sites@2020-06-01' = {
  name: functionAppName
  location: defaultResourceLocation
  kind: 'functionapp'
  properties: {
    serverFarmId: functionappplan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${functionstore.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(functionstore.id, functionstore.apiVersion).keys[0].value}'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: 'InstrumentationKey=${appInsights.properties.InstrumentationKey}'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~3'
        }
      ]
    }
    httpsOnly: true
  }
}
