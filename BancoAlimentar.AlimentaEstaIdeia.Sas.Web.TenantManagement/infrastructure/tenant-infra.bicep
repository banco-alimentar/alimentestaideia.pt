@description('The foodbank tenant identifier')
param tenantId string

@description('The location of the resources')
param location string = resourceGroup().location

@description('The object id of the managed identity that the web site uses')
param webIdentityObectId string

@description('The collation of the SQL database')
param sqlCollation string

@description('The maximum size of the SQL database in bytes')
param sqlMaxSizeBytes string

@description('The username of the sql server admin login')
param sqlServerAdminUser string

@description('The password of the sql server admin login')
@secure()
param sqlServerAdminPassword string

var suffix = take(uniqueString(tenantId), 6)

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

resource storageaccount 'Microsoft.Storage/storageAccounts@2021-02-01' = {
  name: 'sa${suffix}'
  location: location
  tags: {
    CustomerTenant: tenantId
  }
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'BlobStorage'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
  }
}

resource sqlServer 'Microsoft.Sql/servers@2014-04-01' = {
  name: 'sql-svr-${suffix}'
  location: location
  tags: {
    CustomerTenant: tenantId
  }
  properties: {
    administratorLogin: sqlServerAdminUser
    administratorLoginPassword: sqlServerAdminPassword
  }
}

resource sqlServerDatabase 'Microsoft.Sql/servers/databases@2014-04-01' = {
  parent: sqlServer
  name: 'sql-db-${suffix}'
  location: location
  tags: {
    CustomerTenant: tenantId
  }
  properties: {
    collation: sqlCollation
    edition: 'Basic'
    requestedServiceObjectiveName: 'Basic'
  }
}

resource appInsightsComponents 'Microsoft.Insights/components@2020-02-02-preview' = {
  name: 'ai-${suffix}'
  location: location
  tags: {
    CustomerTenant: tenantId
  }
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}
