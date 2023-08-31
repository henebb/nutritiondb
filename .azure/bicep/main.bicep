// Set scope to subcription level. 
// I.e. we deploy things for the subscription. This since we also create the resource group in this script.
// Created resources will later target the created resource group.
targetScope='subscription'

@description('The geographical location for resource group and all resources.')
param location string = deployment().location // take the location from the deployment (per default). 

// First create the resource group. 
// This must be done here in main so we can add resources to it.
// This since different 'targetScope' doesn't really work, read more: https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/deploy-to-subscription?tabs=azure-cli#create-resource-group-and-resources
@description('Create resource group')
resource newRG 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-henebb-nutritiondb-prod-${location}'
  location: location
}

// Create Key vault
@description('Create KeyVault')
module keyVault 'keyvault.bicep' = {
  name: 'newKeyVault'
  scope: newRG // Symbolic name for the resource group created.
  params: {
    keyVaultLocation: location
  }
}

// Store the key vault name in a variable, to be used below.
var keyVaultName = keyVault.outputs.keyVaultName

// Create Cosmos DB
@description('Create Cosmos db')
module cosmosDb 'cosmos.bicep' = {
  name: 'newCosmosDb'
  scope: newRG // Symbolic name for the resource group created.
  params: {
    cosmosLocation: location
    keyVaultName: keyVaultName
  }
}

// Create Azure Function app
@description('Create Azure Function')
module azureFunc 'functions.bicep' = {
  name: 'newAzureFunctions'
  scope: newRG // Symbolic name for the resource group created.
  params: {
    location: location
    keyVaultName: keyVaultName
    cosmosDatabaseName: cosmosDb.outputs.cosmosDatabaseName
    cosmosContainerName: cosmosDb.outputs.cosmosContainerName
    cosmosConnectionStringSecretName: cosmosDb.outputs.cosmosConnectionStringSecretName
  }
}
