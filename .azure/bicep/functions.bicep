@description('Region for all resources')
param location string = resourceGroup().location

@description('Suffix to use for all resource names')
param resNameSuffix string = uniqueString(resourceGroup().id)

@description('Storage account SKU name')
param storageSku string = 'Standard_LRS'

@description('The database name for the Cosmos DB.')
param cosmosDatabaseName string

@description('The container name in the Cosmos DB.')
param cosmosContainerName string

@description('The keyvault secret name for Cosmos DB connection string.')
param cosmosConnectionStringSecretName string

@description('The name of the previously created key vault')
param keyVaultName string

// Create storage account for function app
@description('Storage account')
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'fnstore${replace(resNameSuffix, '-', '')}'
  location: location
  sku: {
    name: storageSku
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        file: {
          keyType: 'Account'
          enabled: true
        }
        blob: {
          keyType: 'Account'
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
    accessTier: 'Hot'
  }
}

// Create App Service Plan for function app
@description('App Service plan')
resource plan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: 'FunctionPlan'
  location: location
  kind: 'functionapp'
  sku: {
    name: 'Y1'
  }
  properties: {}
}

// Fetch the prev created key vault resource.
// This to fetch the vault URI, and to give permission for the Azure function.
@description('Fetch reference to the existing key vault to add the Cosmos connection string')
resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: keyVaultName
}

// Create Function app site
@description('Function App site')
resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: 'fn-${resNameSuffix}'
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'CosmosDb:DatabaseId'
          value: cosmosDatabaseName
        }
        {
          name: 'CosmosDb:ContainerId'
          value: cosmosContainerName
        }
        // The settings below are stored in key vault, set the key vault using reference name, so they can be fetched by app (access policy set below).
        {
          name: 'CosmosDb:ConnectionString'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${cosmosConnectionStringSecretName})'
        }
      ]
    }
  }
  // Specify identity so we can later use '.identity.principalId' for this resource.
  identity: {
    type: 'SystemAssigned'
  }
}

// Now add permission to the keyvault.
@description('Access policy for the created Azure function. To be able to access the created connection string secret')
resource keyVaultPolicies 'Microsoft.KeyVault/vaults/accessPolicies@2023-02-01' = {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        objectId: functionApp.identity.principalId
        // Give permission to fetch secrets.
        permissions: {
          secrets: [
            'get'
          ]
        }
        tenantId: subscription().tenantId
      }
    ]
  }
}
