# ExamHub — Azure Deployment Runbook

Target: **App Service (API)** + **Static Web App (frontend)** + **PostgreSQL Flexible Server**,
all inside resource group **`examhub-rg`** (Central India), subscription **Training-2026**.

Run the steps **in order, in one terminal** (variables must persist). Each step has a
**Verify** check. `[COST]` marks steps that create billable resources.

> The API name is fixed to **`examhub-api-b8d965`** because
> `ExamPlatform.Frontend/src/environments/environment.prod.ts` already points there —
> keeping them in sync means the frontend build needs no edit.

---

## 0. Variables (paste once)

```bash
SUB="e25db867-f63d-489a-8bce-7de21e5f94a1"     # Training-2026
RG="examhub-rg"
LOC="centralindia"
SWA_LOC="eastasia"

API_APP="examhub-api-b8d965"                    # MUST match environment.prod.ts
API_URL="https://${API_APP}.azurewebsites.net"
PLAN="examhub-plan"

DB_SERVER="examhub-db-b8d965"                    # globally unique
DB_NAME="exam_platform_db"
DB_ADMIN="examadmin"
DB_PASS="Exam$(openssl rand -hex 10)Aa1"         # save the printed value!

SWA="examhub-web-b8d965"
JWT_KEY=$(openssl rand -hex 48)

REPO="/Users/sainikitha/Desktop/genspark-training/capstone"
API_PROJ="$REPO/ExamPlatform.Backend/ExamPlatform.API"
FRONT="$REPO/ExamPlatform.Frontend"

echo "SAVE -> DB_PASS=$DB_PASS"
az account set --subscription "$SUB"
```

**Save the `DB_PASS`** — you need it for the seed step and any future DB access.

---

## 1. Resource group (already exists — just verify)

```bash
az group show -n "$RG" --query properties.provisioningState -o tsv     # -> Succeeded
```

---

## 2. [COST] PostgreSQL Flexible Server + database (~3 min)

```bash
az postgres flexible-server create \
  --resource-group "$RG" --name "$DB_SERVER" --location "$LOC" \
  --admin-user "$DB_ADMIN" --admin-password "$DB_PASS" \
  --tier Burstable --sku-name Standard_B1ms --storage-size 32 \
  --version 16 --public-access 0.0.0.0 --yes -o table

az postgres flexible-server db create \
  --resource-group "$RG" --server-name "$DB_SERVER" --database-name "$DB_NAME" -o table

CONN="Host=${DB_SERVER}.postgres.database.azure.com;Port=5432;Database=${DB_NAME};Username=${DB_ADMIN};Password=${DB_PASS};SSL Mode=Require;Trust Server Certificate=true"
```

**Verify:** `az postgres flexible-server show -g "$RG" -n "$DB_SERVER" --query state -o tsv` -> `Ready`

---

## 3. [COST] App Service plan + Web App

```bash
az appservice plan create -g "$RG" -n "$PLAN" --sku B1 --is-linux --location "$LOC" -o table

# Check the name is globally free first (empty output = available):
az webapp list --query "[?name=='$API_APP'].name" -o tsv

az webapp create -g "$RG" --plan "$PLAN" -n "$API_APP" --runtime "DOTNETCORE:10.0" -o table
```

**Verify:** `az webapp show -g "$RG" -n "$API_APP" --query state -o tsv` -> `Running`

---

## 4. App settings (secrets) — fill in 3 values first

Get the LLM secrets from your User Secrets, and use a **new** Gmail app password:

```bash
dotnet user-secrets list --project "$API_PROJ"      # copy LLM:BaseUrl and LLM:ApiKey

LLM_BASEURL="<paste LLM:BaseUrl>"
LLM_APIKEY="<paste LLM:ApiKey>"
SMTP_PASS="<new Gmail app password>"
```

```bash
az webapp config appsettings set -g "$RG" -n "$API_APP" --settings \
  ASPNETCORE_ENVIRONMENT="Production" \
  "ConnectionStrings__DefaultConnection=$CONN" \
  "Jwt__Key=$JWT_KEY" "Jwt__Issuer=ExamPlatform.API" "Jwt__Audience=ExamPlatform.Client" "Jwt__ExpiryMinutes=480" \
  "LLM__BaseUrl=$LLM_BASEURL" "LLM__ApiKey=$LLM_APIKEY" "LLM__Model=claude-sonnet-4-6" \
  "Smtp__Host=smtp.gmail.com" "Smtp__Port=587" "Smtp__Username=sainikinsr@gmail.com" \
  "Smtp__Password=$SMTP_PASS" "Smtp__EnableSsl=true" "Smtp__FromName=Exam Platform" \
  "Database__MigrateOnStartup=true" \
  "Cors__AllowedOrigins__0=https://placeholder.invalid" -o table
```

**Verify:** `az webapp config appsettings list -g "$RG" -n "$API_APP" --query "[].name" -o tsv` lists all keys.

---

## 5. Publish & deploy the API (migrations auto-create the schema on first boot)

```bash
rm -rf "$API_PROJ/publish" "$API_PROJ/publish.zip"
dotnet publish "$API_PROJ" -c Release -o "$API_PROJ/publish"
( cd "$API_PROJ/publish" && zip -r ../publish.zip . >/dev/null )
az webapp deploy -g "$RG" -n "$API_APP" --src-path "$API_PROJ/publish.zip" --type zip
```

**Verify** (Swagger is OFF in Production — use curl; first hit is slow, wait ~40s):
```bash
curl -i "$API_URL/api/categories"       # expect 200 or 401 — NOT 5xx / connection refused
# if it fails: az webapp log tail -g "$RG" -n "$API_APP"
```

---

## 6. Seed your existing data (schema now exists from step 5's migration)

```bash
PGPASSWORD="$DB_PASS" psql \
  "host=${DB_SERVER}.postgres.database.azure.com port=5432 dbname=${DB_NAME} user=${DB_ADMIN} sslmode=require" \
  -v ON_ERROR_STOP=1 -1 -f "$REPO/deploy/seed-demo-content.sql"
```

**Verify:** login works —
```bash
curl -s -o /dev/null -w "%{http_code}\n" -X POST "$API_URL/api/auth/login" \
  -H "Content-Type: application/json" -d '{"emailOrUsername":"admin","password":"admin1234"}'   # -> 200
```

---

## 7. Build & deploy the frontend (Static Web App)

```bash
( cd "$FRONT" && npx ng build )          # uses environment.prod.ts (already correct)

az staticwebapp create -n "$SWA" -g "$RG" --location "$SWA_LOC" --sku Free -o table

SWA_TOKEN=$(az staticwebapp secrets list -n "$SWA" -g "$RG" --query "properties.apiKey" -o tsv)
npx @azure/static-web-apps-cli deploy "$FRONT/dist/ExamPlatform.Frontend/browser" \
  --deployment-token "$SWA_TOKEN" --env production

SWA_URL="https://$(az staticwebapp show -n "$SWA" -g "$RG" --query defaultHostname -o tsv)"
echo "FRONTEND: $SWA_URL"
```

**Verify:** open `$SWA_URL` — login page loads (login itself works after step 8).

---

## 8. Allow the frontend origin through CORS, then restart

```bash
az webapp config appsettings set -g "$RG" -n "$API_APP" \
  --settings "Cors__AllowedOrigins__0=$SWA_URL" -o table
az webapp restart -g "$RG" -n "$API_APP"
```

Wait ~30s, then log in at `$SWA_URL`:
- Admin `admin` / `admin1234` - Examiner `profdemo` / `demo1234` - Candidate `diya` / `diya1234`

---

## Redeploy later (the iteration loop)

- **API code change:** repeat step 5.
- **Frontend change:** repeat step 7 (build + swa deploy).
- **Schema change:** add an EF migration, commit, repeat step 5 — it auto-applies on boot.
- **Reset demo data:** re-run step 6 (idempotent; ON CONFLICT DO NOTHING).

## Tear down (stop all charges)

```bash
az group delete --name examhub-rg --yes --no-wait
```
