@description('The foodbank tenant identifier')
param tenantId string

@description('The location of the resources')
param location string = resourceGroup().location

@description('The object id of the managed identity that the web site uses')
param webIdentityObectId string

var suffix = take(uniqueString(tenantId), 4)

resource keyVault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: 'kv-${suffix}'
  location: location
  tags: {
    CustomerTenant: tenantId
  }
  properties: {
    enabledForDeployment: false
    enabledForTemplateDeployment: false
    enabledForDiskEncryption: false
    tenantId: subscription().tenantId
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: webIdentityObectId
        permissions: {
          secrets: [
            'list'
            'get'
          ]
        }
      }
    ]
    sku: {
      name: 'premium'
      family: 'A'
    }
  }
}
