#!/usr/bin/env bash
# Migra o realm `rufino` do estado anterior a 2026-09-04 para o definido nos
# arquivos versionados desta pasta.
#
#   ./migrate-realm.sh --check     # só lê e relata o que falta (padrão)
#   ./migrate-realm.sh --apply     # aplica
#
# POR QUE NÃO É UM IMPORT DE REALM: importar o realm inteiro por cima de um
# existente não renomeia nada — cria ao lado e deixa o antigo. Pior, perderia
# as atribuições de papel dos usuários e o segredo dos clients confidenciais
# (o export mascara secrets). O que este script faz é RENOMEAR o que já existe,
# criar só o que é novo, e substituir a configuração de autorização em bloco.
#
# ORDEM OBRIGATÓRIA, e ela não é arbitrária: as policies de autorização citam
# papéis por nome (`bill-payment-api/bill-approver-danger`). Importar a
# autorização antes de os papéis existirem falha — ou, pior, cria a policy com
# referência vazia, que nega tudo em silêncio.

set -euo pipefail

KC_URL="${KC_URL:-https://keycloak.couratechsafety.cloud}"
REALM="${REALM:-rufino}"
MODE="${1:---check}"

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PM_AUTHZ="$HERE/people-management-authz-config.json"
BP_AUTHZ="$HERE/bill-payment-authz-config.json"

if [[ -z "${KC_ADMIN_TOKEN:-}" ]]; then
  cat >&2 <<'EOF'
Falta KC_ADMIN_TOKEN. Pegue um token de admin (vale ~60s por padrão):

  export KC_ADMIN_TOKEN=$(curl -s -X POST \
    "https://keycloak.couratechsafety.cloud/realms/master/protocol/openid-connect/token" \
    -d grant_type=password -d client_id=admin-cli \
    -d username=<admin> -d password=<senha> | python -c 'import json,sys;print(json.load(sys.stdin)["access_token"])')

O token expira rápido — se o script falhar com 401 no meio, refaça e rode de novo.
Ele é idempotente: o que já foi aplicado é reconhecido e pulado.
EOF
  exit 1
fi

APPLY=0
[[ "$MODE" == "--apply" ]] && APPLY=1
[[ $APPLY -eq 1 ]] && echo ">>> MODO APLICAR" || echo ">>> modo conferência (nada é alterado)"
echo ">>> $KC_URL / realm $REALM"
echo

api() { # api <método> <caminho> [corpo]
  local method="$1" path="$2" body="${3:-}"
  if [[ -n "$body" ]]; then
    curl -sS -o /tmp/kc-out -w '%{http_code}' -X "$method" \
      -H "Authorization: Bearer $KC_ADMIN_TOKEN" -H 'Content-Type: application/json' \
      --data-binary "$body" "$KC_URL/admin/realms/$REALM$path"
  else
    curl -sS -o /tmp/kc-out -w '%{http_code}' -X "$method" \
      -H "Authorization: Bearer $KC_ADMIN_TOKEN" "$KC_URL/admin/realms/$REALM$path"
  fi
}

get() { api GET "$1" >/dev/null; cat /tmp/kc-out; }

client_uuid() { # o UUID interno, que é o que os endpoints de autorização usam
  get "/clients?clientId=$1" | python -c 'import json,sys; c=json.load(sys.stdin); print(c[0]["id"] if c else "")'
}

step() { echo; echo "── $* ─────────────────────────────────────────"; }
ok()   { echo "   ✔ $*"; }
todo() { echo "   → $*"; }
skip() { echo "   · $*"; }

do_or_report() { # do_or_report <descrição> <método> <caminho> [corpo]
  local desc="$1"; shift
  if [[ $APPLY -eq 0 ]]; then todo "$desc"; return 0; fi
  local code; code="$(api "$@")"
  if [[ "$code" =~ ^2 ]]; then ok "$desc"; else
    echo "   ✘ $desc  (HTTP $code)"; head -c 400 /tmp/kc-out; echo; return 1
  fi
}

# ───────────────────────────────────────────────────────── 1. papel de realm
step "1. Realm role 'developer' (libera as ferramentas de diagnóstico do app)"
if get "/roles" | grep -q '"name" *: *"developer"'; then
  skip "já existe"
else
  do_or_report "criar realm role 'developer'" POST /roles \
    '{"name":"developer","description":"Libera as ferramentas de diagnostico do app (tela /debug). Realm role de proposito: descreve a PESSOA, e nenhuma API a le. Ver CONVENCOES.md secao 4-B."}'
fi

# ───────────────────────────────────────── 2. renomear papéis do PeopleManagement
step "2. Papéis do people-management-api"
PM_ID="$(client_uuid people-management-api)"
[[ -z "$PM_ID" ]] && { echo "   ✘ client people-management-api não encontrado"; exit 1; }
PM_ROLES="$(get "/clients/$PM_ID/roles")"

rename_client_role() { # <uuid do client> <de> <para> <descrição>
  local cid="$1" from="$2" to="$3" desc="$4" roles="$5"
  if echo "$roles" | grep -q "\"name\" *: *\"$to\""; then skip "$to já existe"; return; fi
  if echo "$roles" | grep -q "\"name\" *: *\"$from\""; then
    # PUT sobre o papel existente preserva as ATRIBUIÇÕES. Apagar e recriar as perderia.
    do_or_report "renomear $from → $to" PUT "/clients/$cid/roles/$from" \
      "{\"name\":\"$to\",\"description\":\"$desc\"}"
  else
    do_or_report "criar $to" POST "/clients/$cid/roles" \
      "{\"name\":\"$to\",\"description\":\"$desc\"}"
  fi
}

rename_client_role "$PM_ID" admin people-admin \
  "Configuracao da instalacao: modelos, exigencias por cargo, empresa, departamentos, cargos, funcoes e locais." "$PM_ROLES"
rename_client_role "$PM_ID" doc-send people-doc-operator \
  "Trabalha a documentacao de ponta a ponta e NAO mexe no cadastro de pessoas." "$PM_ROLES"
rename_client_role "$PM_ID" zapsign-webhook people-signature-webhook \
  "Service account do provedor de assinatura. Alcanca UM escopo: document:webhook." "$PM_ROLES"

for pair in \
  "people-viewer:So leitura: consultar e baixar funcionarios, documentos e cadastros." \
  "people-operator:O people-doc-operator MAIS o cadastro de funcionarios. Nao configura a instalacao." \
  "people-reviewer:Valida documento: aprova, reprova, deprecia e marca como nao aplicavel."
do
  name="${pair%%:*}"; desc="${pair#*:}"
  if echo "$PM_ROLES" | grep -q "\"name\" *: *\"$name\""; then skip "$name já existe"; else
    do_or_report "criar $name" POST "/clients/$PM_ID/roles" "{\"name\":\"$name\",\"description\":\"$desc\"}"
  fi
done

# ────────────────────────────────── 3. papéis de alçada do BillPayment (compostos)
step "3. Papéis de alçada do bill-payment-api"
BP_ID="$(client_uuid bill-payment-api)"
[[ -z "$BP_ID" ]] && { echo "   ✘ client bill-payment-api não encontrado"; exit 1; }
BP_ROLES="$(get "/clients/$BP_ID/roles")"

for pair in \
  "bill-approver-attention:Alcada para aprovar boleto de risco Atencao." \
  "bill-approver-danger:Alcada para aprovar boleto de risco Perigo. Cobre Atencao." \
  "bill-approver-extreme:Alcada para aprovar boleto de risco Extremo Perigo. Cobre Perigo e Atencao."
do
  name="${pair%%:*}"; desc="${pair#*:}"
  if echo "$BP_ROLES" | grep -q "\"name\" *: *\"$name\""; then skip "$name já existe"; else
    do_or_report "criar $name" POST "/clients/$BP_ID/roles" "{\"name\":\"$name\",\"description\":\"$desc\"}"
  fi
done

# O composto é o que faz o console mostrar a cobertura real: quem tem -extreme
# aparece cobrindo -danger e -attention, sem ninguém precisar saber a hierarquia.
if [[ $APPLY -eq 1 ]]; then
  # A composição é: o FILHO entra como composite do PAI.
  for pair in "bill-approver-danger:bill-approver-attention" "bill-approver-extreme:bill-approver-danger"; do
    parent="${pair%%:*}"; child="${pair#*:}"
    parent_id="$(get "/clients/$BP_ID/roles/$parent" | python -c 'import json,sys;print(json.load(sys.stdin)["id"])')"
    child_repr="$(get "/clients/$BP_ID/roles/$child")"
    do_or_report "$parent passa a conter $child" POST "/roles-by-id/$parent_id/composites" "[$child_repr]"
  done
else
  todo "compor bill-approver-danger ⊃ attention, e extreme ⊃ danger"
fi

# ─────────────────────────────────────────── 4. configuração de autorização
step "4. Configuração de autorização (recursos, escopos, policies, permissões)"
echo "   Substitui em BLOCO o resource-server do client. Roda DEPOIS dos papéis:"
echo "   as policies citam papel por nome, e com o papel ausente a policy nasce vazia."
do_or_report "importar authz do people-management-api" \
  POST "/clients/$PM_ID/authz/resource-server/import" "$(cat "$PM_AUTHZ")"
do_or_report "importar authz do bill-payment-api" \
  POST "/clients/$BP_ID/authz/resource-server/import" "$(cat "$BP_AUTHZ")"

# ─────────────────────────────────────────────── 5. client de assinatura
step "5. Client 'zapsign' → 'document-signing-client'"
ZAP_ID="$(client_uuid zapsign)"
NEW_ID="$(client_uuid document-signing-client)"
if [[ -n "$NEW_ID" ]]; then
  skip "document-signing-client já existe"
elif [[ -n "$ZAP_ID" ]]; then
  # PUT preserva o UUID interno, o SEGREDO e a service account. Criar um client
  # novo geraria segredo novo e obrigaria a reconfigurar a API.
  REP="$(get "/clients/$ZAP_ID" | python -c '
import json,sys
c=json.load(sys.stdin)
c["clientId"]="document-signing-client"
c["name"]="Provedor de assinatura de documento"
c["description"]="Client da integracao de assinatura. O nome nao cita o fornecedor: trocar de provedor e um adapter novo, nao a renomeacao de um client."
print(json.dumps(c))')"
  do_or_report "renomear o client (preserva segredo e service account)" PUT "/clients/$ZAP_ID" "$REP"
  echo "   ⚠ Depois disto, 'DocumentSigning:ServiceAccount:ClientId' tem que valer document-signing-client."
else
  skip "nem zapsign nem document-signing-client existem"
fi

# ─────────────────────────────────────────────────── 6. client scopes
step "6. Client scopes"
SCOPES="$(get "/client-scopes")"
scope_id() { echo "$SCOPES" | python -c "
import json,sys
for s in json.load(sys.stdin):
    if s['name']=='$1': print(s['id']); break"; }

rename_scope() { # <de> <para> <descrição>
  local from="$1" to="$2" desc="$3"
  if echo "$SCOPES" | grep -q "\"name\" *: *\"$to\""; then skip "$to já existe"; return; fi
  local id; id="$(scope_id "$from")"
  if [[ -z "$id" ]]; then skip "$from não existe"; return; fi
  local rep; rep="$(get "/client-scopes/$id" | python -c "
import json,sys
s=json.load(sys.stdin); s['name']='$to'; s['description']='$desc'
print(json.dumps(s))")"
  # Renomear preserva a ATRIBUIÇÃO aos clients: ela é por id, não por nome.
  do_or_report "renomear client scope $from → $to" PUT "/client-scopes/$id" "$rep"
}

rename_scope people-management-api-scope people-management-access \
  "Acesso ao PeopleManagement: papeis do client e a audience da API."
rename_scope tenant-scope tenant-access \
  "Tenants da pessoa por produto, e a audience do TenantManagement."

echo
echo "   Os passos abaixo mexem em MAPPER, e o script não os automatiza de propósito:"
echo "   errar um mapper de audience derruba a autenticação inteira, e o diff tem que ser lido."
todo "em people-management-access: corrigir a audience 'people_management-api' → 'people-management-api' (underscore→hífen)"
todo "criar o client scope 'bill-payment-access' com dois mappers: 'client roles' e a audience 'bill-payment-api'"
todo "em tenant-access: REMOVER o mapper de audience do bill-payment-api (ele passa a viver no scope próprio)"
todo "em rufino-app: acrescentar 'bill-payment-access' aos Default client scopes"
todo "remover o client scope 'company-scope' e o atributo 'companies' do User Profile (nenhum código o lê desde 2026-09-03)"

# ───────────────────────────────────────────────────── 7. conferência
step "7. Conferência"
PM_ROLES_NOW="$(get "/clients/$PM_ID/roles" | python -c 'import json,sys;print(",".join(sorted(r["name"] for r in json.load(sys.stdin))))')"
BP_ROLES_NOW="$(get "/clients/$BP_ID/roles" | python -c 'import json,sys;print(",".join(sorted(r["name"] for r in json.load(sys.stdin))))')"
echo "   papéis PM: $PM_ROLES_NOW"
echo "   papéis BP: $BP_ROLES_NOW"
echo "   realm roles: $(get "/roles" | python -c 'import json,sys;print(",".join(sorted(r["name"] for r in json.load(sys.stdin))))')"

echo
echo "── Falta você fazer, e nenhum script deveria fazer sozinho ──────────"
echo "  a) Atribuir os papéis aos DOIS usuários. Renomear preserva a atribuição"
echo "     antiga (quem era 'admin' agora é 'people-admin'), mas os papéis NOVOS"
echo "     não se atribuem sozinhos — em especial as três alçadas de risco do"
echo "     BillPayment e o realm role 'developer'."
echo "  b) bill-admin NÃO aprova mais. Quem precisa aprovar recebe bill-approver"
echo "     explicitamente, e a alçada pelo papel do nível."
echo "  c) Conferir logando com cada usuário: um boleto de risco Atenção só é"
echo "     aprovável por quem tem bill-approver-attention ou acima."
