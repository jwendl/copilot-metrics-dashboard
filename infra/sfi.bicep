param name string
param location string
param resourceToken string
param globalLocation string

var userManagedIdentityName = toLower('${name}-umi-${resourceToken}')
var vnetName = toLower('${name}-vnet-${resourceToken}')

resource userManagedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: userManagedIdentityName
  location: location
}

resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.16.20.0/23'
      ]
    }
    subnets: [
      {
        name: 'AzureBastionSubnet'
        properties: {
          addressPrefix: '10.16.20.0/26'
        }
      }
      {
        name: 'vaultsubnet'
        properties: {
          addressPrefix: '10.16.20.64/27'
        }
      }
      {
        name: 'cosmossubnet'
        properties: {
          addressPrefix: '10.16.20.96/27'
        }
      }
      {
        name: 'storagesubnet'
        properties: {
          addressPrefix: '10.16.20.128/27'
        }
      }
      {
        name: 'appsubnet'
        properties: {
          addressPrefix: '10.16.20.160/27'
          delegations: [
            {
              name: 'webappdelegation'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]          
        }
      }
      {
        name: 'funsubnet'
        properties: {
          addressPrefix: '10.16.21.0/27'
          delegations: [
            {
              name: 'webappdelegation'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]          
        }
      }
      {
        name: 'funstoragesubnet'
        properties: {
          addressPrefix: '10.16.21.32/27'
        }
      }
    ]
  }
}

resource kvDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.vaultcore.azure.net'
  location: globalLocation
}

resource kvVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: kvDnsZone
  name: 'kv-vnet-link'
  location: globalLocation
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.id
    }
  }
}

resource blobDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.blob.${az.environment().suffixes.storage}'
  location: globalLocation
}

resource blobVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: blobDnsZone
  name: 'blob-vnet-link'
  location: globalLocation
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.id
    }
  }
}

resource tableDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.table.${az.environment().suffixes.storage}'
  location: globalLocation
}

resource tableVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: tableDnsZone
  name: 'table-vnet-link'
  location: globalLocation
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.id
    }
  }
}

resource queueDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.queue.${az.environment().suffixes.storage}'
  location: globalLocation
}

resource queueVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: queueDnsZone
  name: 'queue-vnet-link'
  location: globalLocation
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.id
    }
  }
}

output userManagedIdentityResourceId string = userManagedIdentity.id
output userManagedIdentityName string = userManagedIdentity.name
output userManagedIdentityClientId string = userManagedIdentity.properties.clientId
output userManagedIdentityPrincipalId string = userManagedIdentity.properties.principalId

output virtualNetworkResourceId string = vnet.id
output vaultSubnetResourceId string = vnet.properties.subnets[1].id
output appSubnetResourceId string = vnet.properties.subnets[4].id
output funSubnetResourceId string = vnet.properties.subnets[5].id
output funStorageSubnetResourceId string = vnet.properties.subnets[6].id
