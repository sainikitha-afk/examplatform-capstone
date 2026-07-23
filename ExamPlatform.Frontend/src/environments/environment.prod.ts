// Production environment (used by `ng build`, via angular.json fileReplacements).
// apiBaseUrl is the public HTTPS URL of the deployed Azure App Service API.
// NOTE: no trailing slash — every caller appends "/api/...".
// This placeholder is replaced with the real App Service URL once it is created
// during provisioning (search for __API_URL__ if it is still here).
export const environment = {
  production: true,
  apiBaseUrl: 'https://examhub-api-nikitha.azurewebsites.net',
};
