targetScope = 'subscription'

@description('The foodbank tenant identifier')
param tenantId string

@description('The name of the tenant')
param tenantName string

@description('The Azure region for resources.')
param location string = deployment().location

@description('The collation of the SQL database')
param sqlCollation string

@description('The username of the sql server admin login')
param sqlServerAdminUser string

@description('The password of the sql server admin login')
@secure()
param sqlServerAdminPassword string

@description('The object id of the identity that the web site uses')
param webIdentityObectId string

var sufix = take(uniqueString(tenantId), 4)

resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'rg-${tenantName}-${sufix}'
  location: location
  tags: {
    CustomerTenant: tenantId
  }
}

module infra 'tenant-infra.bicep' = {
  name: 'tenant-${tenantName}-deployment'
  scope: resourceGroup
  params: {
    tenantId: tenantId
    tenantName: tenantName
    location: location
    webIdentityObectId: webIdentityObectId
    sqlCollation: sqlCollation
    sqlServerAdminUser: sqlServerAdminUser
    sqlServerAdminPassword: sqlServerAdminPassword
  }
}
