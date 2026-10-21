#!/usr/bin/env bash
# Rotate the Gemini API key for ExamHub — local user-secrets AND the Azure Web App.
# The key is never written to a git-tracked file.
#
# Free key: https://aistudio.google.com/apikey   (keys start with AIza... or AQ....)
#
#   ./deploy/set-gemini-key.sh <NEW_KEY>
#
# No code redeploy is needed — the key is configuration only.
set -euo pipefail

RG="examhub-rg"
API_APP="examhub-api-nikitha"
API_URL="https://${API_APP}.azurewebsites.net"
MODEL="gemini-3.5-flash"
GEMINI_URL="https://generativelanguage.googleapis.com/v1beta/models/${MODEL}:generateContent"
API_PROJ="$(cd "$(dirname "$0")/.." && pwd)/ExamPlatform.Backend/ExamPlatform.API"

KEY="${1:-}"
[ -z "$KEY" ] && { read -rsp "New Gemini API key: " KEY; echo; }
[ -z "$KEY" ] && { echo "No key given." >&2; exit 1; }

# ── 1. Prove the key works before changing anything ──────────────────────────
echo "→ validating key against Google…"
CODE=$(curl -s -o /tmp/gk.json -w '%{http_code}' --max-time 30 \
  "https://generativelanguage.googleapis.com/v1beta/models?key=${KEY}&pageSize=1")
if [ "$CODE" != "200" ]; then
  echo "  REJECTED (HTTP $CODE). Nothing was changed." >&2
  head -c 300 /tmp/gk.json >&2; echo >&2; rm -f /tmp/gk.json; exit 1
fi
rm -f /tmp/gk.json
echo "  key is valid."

# ── 2. Apply locally + on Azure ──────────────────────────────────────────────
echo "→ local user-secrets"
dotnet user-secrets set "Gemini:ApiKey" "$KEY"        --project "$API_PROJ" >/dev/null
dotnet user-secrets set "Gemini:ApiUrl" "$GEMINI_URL" --project "$API_PROJ" >/dev/null

echo "→ azure app settings"
az webapp config appsettings set -g "$RG" -n "$API_APP" \
  --settings "Gemini__ApiKey=$KEY" "Gemini__ApiUrl=$GEMINI_URL" -o none

# ── 3. Restart and CONFIRM the coach really answers.
#      This App Service serves stale config after a single restart, so retry.
for attempt in 1 2 3; do
  echo "→ restart + verify (attempt $attempt)…"
  az webapp restart -g "$RG" -n "$API_APP" -o none
  sleep 55
  TOK=$(curl -s --max-time 40 -X POST "$API_URL/api/auth/login" \
        -H 'Content-Type: application/json' \
        -d '{"emailOrUsername":"diya","password":"diya1234"}' \
        | python3 -c 'import sys,json;print(json.load(sys.stdin).get("token",""))' 2>/dev/null || true)
  if [ -n "${TOK:-}" ]; then
    C=$(curl -s -o /dev/null -w '%{http_code}' --max-time 90 -X POST "$API_URL/api/coach/ask" \
        -H "Authorization: Bearer $TOK" -H 'Content-Type: application/json' \
        -d '{"message":"hi","history":[]}')
    if [ "$C" = "200" ]; then
      echo
      echo "✅ AI Coach is answering on $API_URL"
      echo "   Site: https://yellow-ground-0b5ba1500.7.azurestaticapps.net"
      echo "   You can now safely delete the OLD key in Google AI Studio."
      exit 0
    fi
    echo "   coach returned HTTP $C — retrying"
  else
    echo "   API not ready — retrying"
  fi
done

echo "⚠️  Key was set but the coach did not return 200. Check: az webapp log tail -g $RG -n $API_APP" >&2
exit 1
