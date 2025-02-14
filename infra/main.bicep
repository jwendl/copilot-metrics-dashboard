targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param name string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Name of GitHub enterprise')
@minLength(1)
param gitHubEnterpriseName string

@description('Name of GitHub Organization')
@minLength(1)
param gitHubOrganizationName string

@description('GitHub API scope: "enterprise" or "organization"')
@allowed(['enterprise', 'organization'])
param gitHubApiScope string

@description('GitHub App Client Id')
param gitHubClientId string

@description('GitHub App Installation Id')
param gitHubInstallationId string

@secure()
@description('GitHub App PEM File')
param gitHubPemFile string

@description('API version for the GitHub API e.g. 2022-11-28')
@minLength(1)
param gitHubApiVersion string = '2022-11-28'

@description('True to use Test Data instead of calling the real API')
param useTestData bool

@description('List of team names - works with the new Metrics API')
param teamNames array

param resourceGroupName string = ''

var resourceToken = toLower(uniqueString(subscription().id, name, location))
var tags = { 'azd-env-name': name }


// Organize resources in a resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : 'rg-${name}'
  location: location
  tags: tags
}

module resources 'resources.bicep' = {
  name: 'all-resources'
  scope: rg
  params: {
    name: name
    resourceToken: resourceToken
    tags: tags
    location: location
    gitHubClientId: gitHubClientId
    gitHubInstallationId: gitHubInstallationId
    gitHubPemFile: gitHubPemFile
    gitHubEnterpriseName: gitHubEnterpriseName
    gitHubOrganizationName: gitHubOrganizationName
    gitHubApiVersion: gitHubApiVersion
    gitHubApiScope: gitHubApiScope
    teamNames: teamNames
    useTestData: useTestData
  }
}

output APP_URL string = resources.outputs.url
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
