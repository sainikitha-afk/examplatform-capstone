#!/usr/bin/env bash
# ============================================================================
#  ExamHub — Azure deployment runbook
#  Target: App Service (API) + Static Web App (frontend) + PostgreSQL Flexible
#  Subscription: Training-2026
#
#  This is a RUNBOOK, not a fire-and-forget script. Run it SECTION BY SECTION
#  (copy/paste each block), reading the notes. A few values (the API URL, the
#  Static Web App hostname) are only known AFTER a resource is created, so the
#  ordering matters. Steps that create billable resources are marked  💸.
# ============================================================================
set -euo pipefail

# ─────────────────────────────────────────────────────────────────────────
# 0. VARIABLES  — edit these first.
#    NOTE: DB_SERVER and API_APP names must be GLOBALLY UNIQUE across Azure.
#    A random suffix is appended so you don't collide with someone else.
# ─────────────────────────────────────────────────────────────────────────
SUFFIX=$(openssl rand -hex 3)              # e.g. 9f2a1c — keeps names unique
SUBSCRIPTION_ID="e25db867-f63d-489a-8bce-7de21e5f94a1"   # Training-2026

RG="examhub-rg"
LOC="centralindia"                          # App Service + Postgres region
SWA_LOC="eastasia"                          # Static Web App metadata region (Free tier)

DB_SERVER="examhub-db-${SUFFIX}"
DB_NAME="exam_platform_db"
DB_ADMIN="examadmin"
DB_PASS="$(openssl rand -base64 18)Aa1!"    # strong; printed below so you can save it

PLAN="examhub-plan"
API_APP="examhub-api-${SUFFIX}"
SWA="examhub-web-${SUFFIX}"

JWT_KEY="$(openssl rand -base64 48)"        # fresh production signing key

# Repo paths (this script lives in capstone/deploy/)
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
API_PROJ="$REPO_ROOT/ExamPlatform.Backend/ExamPlatform.API"
FRONT_DIR="$REPO_ROOT/ExamPlatform.Frontend"

echo "RG=$RG  DB_SERVER=$DB_SERVER  API_APP=$API_APP  SWA=$SWA"
echo "SAVE THIS DB PASSWORD -> $DB_PASS"

# ─────────────────────────────────────────────────────────────────────────
# 1. Select subscription
# ─────────────────────────────────────────────────────────────────────────
az account set --subscription "$SUBSCRIPTION_ID"

# ─────────────────────────────────────────────────────────────────────────
# 2. Resource group  (everything lives here; delete this = delete it all)
# ─────────────────────────────────────────────────────────────────────────
az group create --name "$RG" --location "$LOC" --output table

# ─────────────────────────────────────────────────────────────────────────
# 3. 💸 PostgreSQL Flexible Server (Burstable B1ms) + database
#    --public-access 0.0.0.0  ==  "Allow public access from Azure services",
#    which is what lets the App Service reach the DB. (~2–4 min to provision.)
# ─────────────────────────────────────────────────────────────────────────
az postgres flexible-server create \
  --resource-group "$RG" \
  --name "$DB_SERVER" \
  --location "$LOC" \
  --admin-user "$DB_ADMIN" \
  --admin-password "$DB_PASS" \
  --tier Burstable \
  --sku-name Standard_B1ms \
  --storage-size 32 \
  --version 16 \
  --public-access 0.0.0.0 \
  --yes --output table

az postgres flexible-server db create \
  --resource-group "$RG" \
  --server-name "$DB_SERVER" \
  --database-name "$DB_NAME" --output table

# Npgsql connection string (SSL is required by Azure PG).
CONN="Host=${DB_SERVER}.postgres.database.azure.com;Port=5432;Database=${DB_NAME};Username=${DB_ADMIN};Password=${DB_PASS};SSL Mode=Require;Trust Server Certificate=true"

# ─────────────────────────────────────────────────────────────────────────
# 4. 💸 App Service plan (Linux B1) + Web App (.NET 10)
# ─────────────────────────────────────────────────────────────────────────
az appservice plan create \
  --resource-group "$RG" --name "$PLAN" \
  --sku B1 --is-linux --location "$LOC" --output table

az webapp create \
  --resource-group "$RG" --plan "$PLAN" --name "$API_APP" \
  --runtime "DOTNETCORE:10.0" --output table

# ─────────────────────────────────────────────────────────────────────────
# 5. API configuration / secrets  (App Settings OVERRIDE appsettings.json).
#    .NET reads nested keys from env vars using "__" (double underscore).
#
#    ⚠️ Fill in the three LLM/SMTP values below before running:
#      - Get LLM__BaseUrl / LLM__ApiKey from your User Secrets:
#          dotnet user-secrets list --project "$API_PROJ"
#      - Smtp__Password: use a NEW Gmail app password (rotate the committed one!)
#    Cors origin is set to a placeholder now and UPDATED in step 8 once the
#    Static Web App URL exists.
# ─────────────────────────────────────────────────────────────────────────
LLM_BASEURL="<PASTE LLM:BaseUrl FROM USER SECRETS>"
LLM_APIKEY="<PASTE LLM:ApiKey FROM USER SECRETS>"
SMTP_USER="sainikinsr@gmail.com"
SMTP_PASS="<NEW GMAIL APP PASSWORD>"

az webapp config appsettings set \
  --resource-group "$RG" --name "$API_APP" \
  --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    "ConnectionStrings__DefaultConnection=$CONN" \
    "Jwt__Key=$JWT_KEY" \
    "Jwt__Issuer=ExamPlatform.API" \
    "Jwt__Audience=ExamPlatform.Client" \
    "Jwt__ExpiryMinutes=480" \
    "LLM__BaseUrl=$LLM_BASEURL" \
    "LLM__ApiKey=$LLM_APIKEY" \
    "LLM__Model=claude-sonnet-4-6" \
    "Smtp__Host=smtp.gmail.com" \
    "Smtp__Port=587" \
    "Smtp__Username=$SMTP_USER" \
    "Smtp__Password=$SMTP_PASS" \
    "Smtp__EnableSsl=true" \
    "Smtp__FromName=Exam Platform" \
    "Database__MigrateOnStartup=true" \
    "Cors__AllowedOrigins__0=https://placeholder.invalid" \
  --output table

# ─────────────────────────────────────────────────────────────────────────
# 6. Publish & deploy the API  (migrations auto-run on first boot → tables created)
# ─────────────────────────────────────────────────────────────────────────
rm -rf "$API_PROJ/publish" "$API_PROJ/publish.zip"
dotnet publish "$API_PROJ" -c Release -o "$API_PROJ/publish"
( cd "$API_PROJ/publish" && zip -r ../publish.zip . >/dev/null )

az webapp deploy \
  --resource-group "$RG" --name "$API_APP" \
  --src-path "$API_PROJ/publish.zip" --type zip

API_URL="https://${API_APP}.azurewebsites.net"
echo "API deployed at: $API_URL"
# Smoke-test (Swagger is off in Production; hit a lightweight public route):
# curl -i "$API_URL/api/categories"      # expect 200 or 401, NOT a 5xx/connection error

# ─────────────────────────────────────────────────────────────────────────
# 7. Point the frontend at the live API, rebuild, and create the Static Web App
# ─────────────────────────────────────────────────────────────────────────
# 7a. Write the real API URL into environment.prod.ts (no trailing slash).
sed -i '' "s#https://__API_URL__#${API_URL}#" \
  "$FRONT_DIR/src/environments/environment.prod.ts"
grep apiBaseUrl "$FRONT_DIR/src/environments/environment.prod.ts"   # verify

# 7b. Production build (uses environment.prod.ts via angular.json fileReplacements).
( cd "$FRONT_DIR" && npx ng build )

# 7c. 💸 Create the Static Web App (Free tier — $0).
az staticwebapp create \
  --name "$SWA" --resource-group "$RG" \
  --location "$SWA_LOC" --sku Free --output table

# 7d. Deploy the built site (the 'browser' subfolder is the web root).
SWA_TOKEN=$(az staticwebapp secrets list \
  --name "$SWA" --resource-group "$RG" \
  --query "properties.apiKey" -o tsv)

npx @azure/static-web-apps-cli deploy \
  "$FRONT_DIR/dist/ExamPlatform.Frontend/browser" \
  --deployment-token "$SWA_TOKEN" --env production

SWA_URL="https://$(az staticwebapp show --name "$SWA" --resource-group "$RG" --query defaultHostname -o tsv)"
echo "Frontend live at: $SWA_URL"

# ─────────────────────────────────────────────────────────────────────────
# 8. Allow the frontend origin through CORS, then restart the API
# ─────────────────────────────────────────────────────────────────────────
az webapp config appsettings set \
  --resource-group "$RG" --name "$API_APP" \
  --settings "Cors__AllowedOrigins__0=$SWA_URL" --output table

az webapp restart --resource-group "$RG" --name "$API_APP"

echo ""
echo "=========================================================="
echo "  DONE"
echo "  Frontend : $SWA_URL"
echo "  API      : $API_URL"
echo "  DB pass  : $DB_PASS   (save it somewhere safe)"
echo "=========================================================="

# ─────────────────────────────────────────────────────────────────────────
#  REDEPLOY LATER (after code changes — this is the loop you asked about):
#    • API only:      repeat step 6  (dotnet publish → zip → az webapp deploy)
#    • Frontend only: repeat step 7b + 7d  (ng build → swa deploy)
#    • Schema change: create a migration locally, commit; step 6 redeploy runs
#                     it automatically on boot (Database:MigrateOnStartup).
#
#  TEAR DOWN (stop all charges):
#    az group delete --name examhub-rg --yes --no-wait
# ─────────────────────────────────────────────────────────────────────────
