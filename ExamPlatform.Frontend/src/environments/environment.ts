// Development / test environment.
// apiBaseUrl MUST stay exactly "http://localhost:5014" — the local API origin and
// the value the .spec.ts HttpTestingController expectations are written against.
// The production build swaps this file for environment.prod.ts (see angular.json
// fileReplacements), so the deployed app points at the Azure API instead.
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5014',
};
