@description('Cosmos location')
param cosmosLocation string = resourceGroup().location

@description('Cosmos DB account name')
param accountName string = 'cosmos-${uniqueString(resourceGroup().id)}'

@description('The name for the Core (SQL) database')
param databaseName string = 'nutrition-data'

@description('The name for the Core (SQL) API container')
param containerName string = 'Nutritions'

@description('The name of the previously created key vault')
param keyVaultName string

@description('The name of the Cosmos Ccnnection string secret')
param cosmosConnectionStringSecretName string = 'CosmosConnectionString'

// Free tier Azure Cosmos DB account
@description('Cosmos DB account')
resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: toLower(accountName)
  location: cosmosLocation
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: cosmosLocation
      }
    ]
  }
}

@description('Cosmos DB')
resource cosmosDB 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
  parent: cosmosAccount
  name: toLower(databaseName)
  properties: {
    resource:{
      id: databaseName
    }
    options: {
      throughput: 400
    }
  }
}

@description('Add container to DB')
resource cosmosContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2021-10-15' = {
  parent: cosmosDB
  name: containerName
  properties: {
    resource: {
      id: containerName
      // For simplicity, use the mandatory "id" field as partition key.
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
    }
  }
}

@description('Fetch reference to the existing key vault to add the Cosmos connection string')
resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: keyVaultName
}

@description('Add the Cosmos connection string to key vault')
resource keyVaultSecret 'Microsoft.KeyVault/vaults/secrets@2021-10-01' = {
  parent: keyVault
  name: cosmosConnectionStringSecretName
  properties: {
    value: cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString
  }
}

output cosmosDatabaseName string = databaseName
output cosmosContainerName string = cosmosContainer.name
output cosmosConnectionStringSecretName string = cosmosConnectionStringSecretName
