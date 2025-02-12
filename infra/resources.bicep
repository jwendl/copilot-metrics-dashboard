param name string = 'azurechat-demo'
param resourceToken string

@secure()
param gitHubPemFile string

param location string = resourceGroup().location

param githubEnterpriseName string

param githubOrganizationName string

param githubAPIVersion string

param githubAPIScope string

param useTestData bool = false

param teamNames array = []

param tags object = {}

var shortName = take(toLower(replace(name, '-', '')), 5)

var cosmosName = toLower('${name}-metrics-${resourceToken}')
var webappName = toLower('${name}-dashboard-${resourceToken}')
var storageName = toLower('${shortName}${resourceToken}')
var functionAppName = toLower('${name}-ingest-${resourceToken}')
var appserviceName = toLower('${name}-dashboard-${resourceToken}')

// keyvault name must be less than 24 chars - token is 13
var keyVaultName = toLower('${shortName}-kv-${resourceToken}')
var logWorkspaceName = toLower('${name}-la-${resourceToken}')
var appinsightsName = toLower('${name}-appi-${resourceToken}')
var diagnosticSettingName = 'AppServiceConsoleLogs'

var keyVaultPrivateEndpointName = toLower('${name}-kvpe-${resourceToken}')
var keyVaultPrivateDnsZoneGroupName = toLower('${name}-kvpdns-${resourceToken}')
var funBlobPrivateEndpointName = toLower('${name}-blobpe-${resourceToken}')
var funBlobPrivateDnsZoneGroupName = toLower('${name}-blobpdns-${resourceToken}')
var cosmosPrivateEndpointName = toLower('${name}-cosmospe-${resourceToken}')
var cosmosPrivateDnsZoneGroupName = toLower('${name}-cosmospdns-${resourceToken}')

//'/${subscription().id}/resourceGroups/${resourceGroup().name}/providers/Microsoft.DocumentDB/databaseAccounts/${cosmosDbAccount.name}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
resource cosmosDbReaderRoleDefinition 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-11-15' existing = {
  parent: cosmosDbAccount
  name: '00000000-0000-0000-0000-000000000001'
}

//'/${subscription().id}/resourceGroups/${resourceGroup().name}/providers/Microsoft.DocumentDB/databaseAccounts/${cosmosDbAccount.name}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000001'
resource cosmosDbContributorRoleDefinition 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-11-15' existing = {
  parent: cosmosDbAccount
  name: '00000000-0000-0000-0000-000000000002'
}

resource keyVaultSecretsOfficerRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7'
}

resource storageDataWriterRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
}

var databaseName = 'platform-engineering'
var orgContainerName = 'history'
var metricsContainerName = 'metrics_history'
var seatsContainerName = 'seats_history'
var userManagedIdentityResourceId = resourceId(resourceGroup().name, 'Microsoft.ManagedIdentity/userAssignedIdentities', toLower('${name}-umi-${resourceToken}'))

module sfi 'sfi.bicep' = {
  name: 'sfi-deployment'
  params: {
    name: name
    location: location
    resourceToken: resourceToken
    globalLocation: 'global'
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: appserviceName
  location: location
  tags: tags
  properties: {
    reserved: true
  }
  sku: {
    name: 'P0v3'
    tier: 'Premium0V3'
    size: 'P0v3'
    family: 'Pv3'
    capacity: 1
  }
  kind: 'linux'
}

var teamNameAppSettings = [
  for (teamName, idx) in teamNames: {
    name: 'GITHUB_METRICS__Teams__${idx}'
    value: teamName
  }
]

resource copilotDataFunction 'Microsoft.Web/sites@2024-04-01' = {
  name: functionAppName
  tags: union(tags, { 'azd-service-name': 'ingestion' })
  kind: 'functionapp'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userManagedIdentityResourceId}': {}
    }
  }
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    virtualNetworkSubnetId: sfi.outputs.funSubnetResourceId
    keyVaultReferenceIdentity: userManagedIdentityResourceId
    siteConfig: {
      alwaysOn: true
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      appSettings: union(teamNameAppSettings, [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'AzureWebJobsStorage__accountname'
          value: functionsStorage.name
        }
        {          
          name: 'AzureWebJobsStorage__clientId'          
          value: sfi.outputs.userManagedIdentityClientId
        }
        {          
          name: 'AzureWebJobsStorage__credential'          
          value: 'managedidentity'
        }
        {
          name: 'AzureWebJobsStorage__blobServiceUri'
          value: 'https://${functionsStorage.name}.blob.${az.environment().suffixes.storage}'
        }
        {
          name: 'AzureWebJobsStorage__queueServiceUri'
          value: 'https://${functionsStorage.name}.queue.${az.environment().suffixes.storage}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'AZURE_COSMOSDB_ENDPOINT__accountEndpoint'
          value: cosmosDbAccount.properties.documentEndpoint
        }
        {
          name: 'GITHUB_TOKEN'
          value: '@Microsoft.KeyVault(VaultName=${kv.name};SecretName=${kv::GITHUB_PEM.name})'
        }
        {
          name: 'GITHUB_ENTERPRISE'
          value: githubEnterpriseName
        }
        {
          name: 'GITHUB_ORGANIZATION'
          value: githubOrganizationName
        }
        {
          name: 'GITHUB_API_VERSION'
          value: githubAPIVersion
        }
        {
          name: 'GITHUB_API_SCOPE'
          value: githubAPIScope
        }
        {
          name: 'GITHUB_METRICS__UseTestData'
          value: '${useTestData}'
        }
      ])
    }
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webappName
  location: location
  tags: union(tags, { 'azd-service-name': 'frontend' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userManagedIdentityResourceId}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    virtualNetworkSubnetId: sfi.outputs.appSubnetResourceId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'node|20-lts'
      alwaysOn: true
      appCommandLine: 'next start'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AZURE_KEY_VAULT_NAME'
          value: keyVaultName
        }
        {
          name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
          value: 'true'
        }
        {
          name: 'AZURE_COSMOSDB_ENDPOINT'
          value: cosmosDbAccount.properties.documentEndpoint
        }
        {
          name: 'GITHUB_TOKEN'
          value: '@Microsoft.KeyVault(VaultName=${kv.name};SecretName=${kv::GITHUB_PEM.name})'
        }
        {
          name: 'GITHUB_ENTERPRISE'
          value: githubEnterpriseName
        }
        {
          name: 'GITHUB_ORGANIZATION'
          value: githubOrganizationName
        }
        {
          name: 'GITHUB_API_VERSION'
          value: githubAPIVersion
        }
        {
          name: 'GITHUB_API_SCOPE'
          value: githubAPIScope
        }
        {
          name: 'USER_ASSIGNED_IDENTITY_CLIENT_ID'
          value: sfi.outputs.userManagedIdentityClientId
        }
      ]
    }
  }

  resource configLogs 'config' = {
    name: 'logs'
    properties: {
      applicationLogs: { fileSystem: { level: 'Verbose' } }
      detailedErrorMessages: { enabled: true }
      failedRequestsTracing: { enabled: true }
      httpLogs: { fileSystem: { enabled: true, retentionInDays: 1, retentionInMb: 35 } }
    }
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-12-01-preview' = {
  name: logWorkspaceName
  location: location
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appinsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    RetentionInDays: 30
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource webDiagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: diagnosticSettingName
  scope: webApp
  properties: {
    workspaceId: logAnalyticsWorkspace.id
    logs: [
      {
        category: 'AppServiceConsoleLogs'
        enabled: true
      }
    ]
    metrics: []
  }
}

resource kvWebAppPermissions 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(kv.id, webApp.name, keyVaultSecretsOfficerRoleDefinition.id)
  scope: kv
  properties: {
    principalId: sfi.outputs.userManagedIdentityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultSecretsOfficerRoleDefinition.id
  }
}

resource kv 'Microsoft.KeyVault/vaults@2021-06-01-preview' = {
  name: keyVaultName
  location: location
  tags: {
    PurgeProtectionEnabledforAKV_Exemption: 'true'
    VirtualNetworkEndPointAKV_Exemption: 'true'
  }
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enabledForDeployment: false
    enabledForDiskEncryption: true
    enabledForTemplateDeployment: false
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Deny'
    }
  }

  resource GITHUB_PEM 'secrets' = {
    name: 'GITHUB-PEM'
    properties: {
      contentType: 'text/plain'
      value: gitHubPemFile
    }
  }
}

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: cosmosName
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    disableKeyBasedMetadataWriteAccess: true
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2022-05-15' = {
  name: databaseName
  parent: cosmosDbAccount
  properties: {
    resource: {
      id: databaseName
    }
  }
}

resource historyContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
  name: orgContainerName
  parent: database
  properties: {
    resource: {
      id: orgContainerName
      partitionKey: {
        paths: [
          '/Month'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource metricsHistoryContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
  name: metricsContainerName
  parent: database
  properties: {
    resource: {
      id: metricsContainerName
      partitionKey: {
        paths: [
          '/date'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource seatsHistoryContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-05-15' = {
  name: seatsContainerName
  parent: database
  properties: {
    resource: {
      id: seatsContainerName
      partitionKey: {
        paths: [
          '/date'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource cosmosDbDataContributor 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = {
  name: guid(cosmosDbAccount.id, copilotDataFunction.name, 'DataContributor')
  parent: cosmosDbAccount
  properties: {
    principalId: sfi.outputs.userManagedIdentityPrincipalId
    roleDefinitionId: cosmosDbContributorRoleDefinition.id
    scope: cosmosDbAccount.id
  }
}

resource cosmosDbDataReader 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = {
  name: guid(cosmosDbAccount.id, webApp.name, 'DataReader')
  parent: cosmosDbAccount
  properties: {
    principalId: sfi.outputs.userManagedIdentityPrincipalId
    roleDefinitionId: cosmosDbReaderRoleDefinition.id
    scope: cosmosDbAccount.id
  }
}

resource functionsStorage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  location: location
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    encryption: {
      keySource: 'Microsoft.Storage'
      requireInfrastructureEncryption: true
      services: {
        blob: {
          enabled: true
          keyType: 'Account'
        }
        table: {
          enabled: true
          keyType: 'Account'
        }
      }
    }
    keyPolicy: {
      keyExpirationPeriodInDays: 7
    }
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
    }
  }
}

resource storageDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(functionsStorage.id, copilotDataFunction.name, 'DataContributor')
  scope: functionsStorage
  properties: {
    principalId: sfi.outputs.userManagedIdentityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: storageDataWriterRoleDefinition.id
  }
}


resource keyVaultPrivateEndpoint 'Microsoft.Network/privateEndpoints@2024-05-01' = {
  name: keyVaultPrivateEndpointName
  location: location
  properties: {
    subnet: {
      id: sfi.outputs.vaultSubnetResourceId
    }
    privateLinkServiceConnections: [
      {
        name: 'KeyVaultPrivateLinkConnection'
        properties: {
          privateLinkServiceId: kv.id
          groupIds: [ 'vault' ]
        }
      }
    ]
  }

  resource keyVaultPrivateDnsZoneGroup 'privateDnsZoneGroups' = {
    name: keyVaultPrivateDnsZoneGroupName
    properties: {
      privateDnsZoneConfigs: [
        {
          name: 'config'
          properties: { privateDnsZoneId: sfi.outputs.vaultPrivateDnsZoneResourceId }
        }
      ]
    }
  }
}

resource storagePrivateEndpointBlob 'Microsoft.Network/privateEndpoints@2024-05-01' = {
  name: funBlobPrivateEndpointName
  location: location
  properties: {
    privateLinkServiceConnections: [
      { 
        name: 'BlobStoragePrivateLinkConnection'
        properties: {
          groupIds: [
            'blob'
          ]
          privateLinkServiceId: functionsStorage.id
        }
      }
    ]
    subnet: {
      id: sfi.outputs.funStorageSubnetResourceId
    }
  }

  resource pvtEndpointDnsGroup 'privateDnsZoneGroups' = {
    name: funBlobPrivateDnsZoneGroupName
    properties: {
      privateDnsZoneConfigs: [
        {
          name: 'ConfigStoragePrivateEndpoint'
          properties: {
            privateDnsZoneId: sfi.outputs.blobPrivateDnsZoneResourceId
          }
        }
      ]
    }
  }
}

resource cosmosPrivateEndpoint 'Microsoft.Network/privateEndpoints@2024-05-01' = {
  name: cosmosPrivateEndpointName
  location: location
  properties: {
    privateLinkServiceConnections: [
      { 
        name: 'CosmosPrivateLinkConnection'
        properties: {
          groupIds: [
            'sql'
          ]
          privateLinkServiceId: cosmosDbAccount.id
        }
      }
    ]
    subnet: {
      id: sfi.outputs.cosmosDbSubnetResourceId
    }
  }

  resource cosmosEndpointDnsGroup 'privateDnsZoneGroups' = {
    name: cosmosPrivateDnsZoneGroupName
    properties: {
      privateDnsZoneConfigs: [
        {
          name: 'CosmosPrivateEndpoint'
          properties: {
            privateDnsZoneId: sfi.outputs.cosmosDbPrivateDnsZoneResourceId
          }
        }
      ]
    }
  }
}

output url string = 'https://${webApp.properties.defaultHostName}'
