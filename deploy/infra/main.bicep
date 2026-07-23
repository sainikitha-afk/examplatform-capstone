targetScope = 'resourceGroup'

@description('Azure region')
param location string = resourceGroup().location

@description('App Service name')
param appServiceName string

@description('App Service Plan name')
param appServicePlanName string

@description('PostgreSQL server name')
param postgresServerName string

@description('Database name')
param databaseName string = 'exam_platform_db'

@description('Administrator username')
param postgresAdmin string

@secure()
@description('Administrator password')
param postgresPassword string

@description('Static Web App name')
param staticWebAppName string