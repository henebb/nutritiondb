@description('Name of the key vault')
param keyVaultName string = 'kv-${uniqueString(resourceGroup().id)}'
@description('Key vault location')
param keyVaultLocation string = resourceGroup().location

@description('Create keyvault')
resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: keyVaultName
  location: keyVaultLocation
  properties: {
    enabledForDeployment: true
    tenantId: tenant().tenantId
    accessPolicies: [
    ]
    sku: {
      name: 'standard'
      family: 'A'
    }
  }
}

output keyVaultName string = keyVault.name

// To purge a soft-deleted key vault (change name to correct):
//   `az keyvault purge --name kv-5u2rhuhnfrfos`
// This can be handy if the resource group is deleted. Then the key vault is just soft-deleted. And this script will fail.
