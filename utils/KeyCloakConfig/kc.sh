# helper: renova o token e expoe A() para leitura
R=$(tr -d ' \r\n' < .kc-refresh)
RESP=$(curl -sS -X POST "https://keycloak.couratechsafety.cloud/realms/master/protocol/openid-connect/token" \
  -d grant_type=refresh_token -d client_id=admin-cli --data-urlencode "refresh_token=$R")
T=$(python -c 'import json,sys;print(json.load(sys.stdin).get("access_token",""))' <<<"$RESP")
NEWR=$(python -c 'import json,sys;print(json.load(sys.stdin).get("refresh_token",""))' <<<"$RESP")
if [ -z "$T" ]; then echo "FALHOU renovar:"; head -c 300 <<<"$RESP"; exit 1; fi
printf '%s' "$T" > .kc-token; [ -n "$NEWR" ] && printf '%s' "$NEWR" > .kc-refresh
export KCT="$T"
A() { curl -s -H "Authorization: Bearer $KCT" "https://keycloak.couratechsafety.cloud/admin/realms/rufino$1"; }
