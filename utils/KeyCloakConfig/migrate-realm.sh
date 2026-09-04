#!/usr/bin/env bash
# Migra o realm `rufino` do estado anterior a 2026-09-04 para o definido nos
# arquivos versionados desta pasta.
#
#   ./migrate-realm.sh --check     # só lê e relata o que falta (padrão)
#   ./migrate-realm.sh --apply     # aplica
#
# POR QUE NÃO É UM IMPORT DE REALM: importar o realm inteiro por cima de um
# existente não renomeia nada — cria ao lado e deixa o antigo. Pior, perderia as
# atribuições de papel dos usuários e o segredo dos clients confidenciais (o
# export mascara secrets). Este script RENOMEIA o que já existe, cria só o que é
# novo, substitui a configuração de autorização e remove o que sobrou dela.
#
# ORDEM OBRIGATÓRIA, e ela não é arbitrária: as policies de autorização citam
# papéis por nome (`bill-payment-api/bill-approver-danger`). Importar a
# autorização antes de os papéis existirem cria a policy com referência vazia,
# que nega tudo em silêncio.
#
# TUDO AQUI FOI MEDIDO contra um Keycloak 26.3 local em 2026-09-04, depois de o
# import do realm local falhar três vezes seguidas por defeitos diferentes.

set -euo pipefail

KC_URL="${KC_URL:-https://keycloak.couratechsafety.cloud}"
REALM="${REALM:-rufino}"
MODE="${1:---check}"

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PM_AUTHZ="$HERE/people-management-authz-config.json"
BP_AUTHZ="$HERE/bill-payment-authz-config.json"

# O token pode vir da variavel de ambiente OU do arquivo .kc-token ao lado deste
# script. O arquivo existe porque variavel exportada num terminal NAO atravessa para
# outro processo: quem gera o token no PowerShell e quem roda o script no bash sao
# shells diferentes. O arquivo esta no .gitignore.
if [[ -z "${KC_ADMIN_TOKEN:-}" && -f "$HERE/.kc-token" ]]; then
  KC_ADMIN_TOKEN="$(tr -d " 
" < "$HERE/.kc-token")"
fi

if [[ -z "${KC_ADMIN_TOKEN:-}" ]]; then
  cat >&2 <<'EOF'
Falta o token de admin. Gere um (vale ~60s) e grave em .kc-token.

PowerShell (o shell que voce usa):

  $c = Get-Credential -Message 'admin do Keycloak'
  $r = Invoke-RestMethod -Method Post -Uri "https://keycloak.couratechsafety.cloud/realms/master/protocol/openid-connect/token" -Body @{
        grant_type='password'; client_id='admin-cli'
        username=$c.UserName; password=$c.GetNetworkCredential().Password }
  $r.access_token | Set-Content -NoNewline utils\KeyCloakConfig\.kc-token

Get-Credential abre um prompt: a senha nao passa pela linha de comando nem pelo
historico. O arquivo .kc-token esta no .gitignore; apague depois de migrar.

EOF
  exit 1
fi

APPLY=0
[[ "$MODE" == "--apply" ]] && APPLY=1
if [[ $APPLY -eq 1 ]]; then echo ">>> MODO APLICAR"; else echo ">>> modo conferência (nada é alterado)"; fi
echo ">>> $KC_URL / realm $REALM"

# Confere os ARQUIVOS antes de tocar no realm. O validador pega os três defeitos
# que derrubaram o import local: descrição acima do limite da coluna, papel citado
# que não existe, e vínculo apontando para nome antigo.
if ! python "$HERE/validate-realm.py" "$HERE/RufinoRealm/realm-import-2026-08-18.json"; then
  echo
  echo "✘ O arquivo de realm tem erro. Corrija antes de migrar — o mesmo defeito"
  echo "  quebraria a nuvem, onde não dá para recriar o realm."
  exit 1
fi
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

# `tr -d` porque o print do Python no Windows emite CRLF: o  fica preso no nome
# lido do realm, o grep normaliza o do lado do arquivo, NADA casa — e a limpeza
# conclui que tudo e' obsoleto. Medido em 2026-09-04, apagando a configuracao
# inteira do realm local num ensaio. Na nuvem teria sido irreversivel.
nomes_do_arquivo() { # <arquivo> <resources|policies|scopes>
  python -c 'import json,io,sys; d=json.load(io.open(sys.argv[1],encoding="utf-8-sig")); print("\n".join(x["name"] for x in d.get(sys.argv[2],[])))' "$1" "$2" | tr -d '\r'
}

# O endpoint de `resource` devolve a chave `_id`; policy e scope devolvem `id`.
nomes_do_realm() { # <uuid> <policy|resource|scope>  ->  id<TAB>nome
  get "/clients/$1/authz/resource-server/$2?max=200" \
    | python -c 'import json,sys; [print((x.get("id") or x["_id"])+"\t"+x["name"]) for x in json.load(sys.stdin)]' \
    | tr -d '\r'
}

# O import MESCLA — não substitui. MEDIDO em 2026-09-04: um recurso plantado no
# realm sobreviveu ao import de um arquivo que não o continha. Sem a limpeza, a
# nuvem ficaria com as policies ANTIGAS do PeopleManagement ("Admin Policy",
# "Doc Send Policy", "Employee Permission"...) ainda concedendo — e, como os
# papéis foram RENOMEADOS e o id interno delas é o mesmo, elas continuariam
# apontando para eles. O modelo novo seria decorativo.
#
# A limpeza roda DEPOIS do import, nunca antes: apagar primeiro deixaria o client
# sem autorização nenhuma se o import falhasse no meio.
import_authz() { # <uuid> <arquivo> <rótulo>
  local cid="$1" arquivo="$2" rotulo="$3"

  if [[ $APPLY -eq 0 ]]; then
    todo "importar authz de $rotulo, e remover o que sobrar fora do arquivo"
    return 0
  fi

  # --data-binary @arquivo, NUNCA "$(cat arquivo)": a interpolação do shell
  # corrompe o JSON e o Keycloak responde 400 "Cannot parse the JSON".
  local code
  code="$(curl -sS -o /tmp/kc-out -w '%{http_code}' -X POST \
    -H "Authorization: Bearer $KC_ADMIN_TOKEN" -H 'Content-Type: application/json' \
    --data-binary "@$arquivo" \
    "$KC_URL/admin/realms/$REALM/clients/$cid/authz/resource-server/import")"

  if [[ ! "$code" =~ ^2 ]]; then
    echo "   ✘ importar authz de $rotulo  (HTTP $code)"; head -c 400 /tmp/kc-out; echo; return 1
  fi
  ok "importar authz de $rotulo"

  local chave esperados removidos id nome
  for tipo in policy resource scope; do
    case "$tipo" in
      policy)   chave=policies  ;;
      resource) chave=resources ;;
      scope)    chave=scopes    ;;
    esac

    esperados="$(nomes_do_arquivo "$arquivo" "$chave")"

    # Lista vazia NUNCA autoriza apagar: significa que o arquivo nao trouxe o que se
    # esperava (chave errada, JSON mudou de forma), e apagar tudo seria a leitura mais
    # destrutiva possivel de um defeito de leitura.
    if [[ -z "$esperados" ]]; then
      echo "   ✘ $rotulo: o arquivo nao declara nenhum '$chave' — limpeza ABORTADA."
      echo "     Apagar tudo por causa de uma lista vazia seria destruir a autorizacao."
      return 1
    fi

    removidos=0

    while IFS=$'\t' read -r id nome; do
      [[ -z "${id:-}" ]] && continue
      # "Default Resource" é do Keycloak; o arquivo do BillPayment não o declara e
      # apagá-lo tiraria um padrão que o console espera encontrar.
      [[ "$nome" == "Default Resource" ]] && continue
      if ! grep -qxF -- "$nome" <<<"$esperados"; then
        curl -sS -o /dev/null -X DELETE -H "Authorization: Bearer $KC_ADMIN_TOKEN" \
          "$KC_URL/admin/realms/$REALM/clients/$cid/authz/resource-server/$tipo/$id"
        echo "     - removido $tipo obsoleto: '$nome'"
        removidos=$((removidos + 1))
      fi
    done < <(nomes_do_realm "$cid" "$tipo")

    [[ $removidos -eq 0 ]] && skip "$rotulo: nenhum $tipo obsoleto"
  done
}

# ─────────────────────────────────────────────────────────── 0. ponto de retorno
# Na nuvem nao existe "apaga o realm e reimporta": o export vai para um arquivo
# ANTES de qualquer escrita, e e' o unico caminho de volta se algo sair errado.
# O export MASCARA os segredos dos clients confidenciais — ele restaura papeis,
# policies e recursos, nunca credenciais.
step "0. Ponto de retorno"
if [[ $APPLY -eq 1 ]]; then
  BACKUP="$HERE/backup-$REALM-$(date +%Y%m%d-%H%M%S).json"
  codigo="$(curl -sS -o "$BACKUP" -w '%{http_code}' -X POST \
    -H "Authorization: Bearer $KC_ADMIN_TOKEN" -H 'Content-Type: application/json' \
    "$KC_URL/admin/realms/$REALM/partial-export?exportClients=true&exportGroupsAndRoles=true")"
  if [[ ! "$codigo" =~ ^2 ]]; then
    echo "   ✘ nao consegui exportar o realm (HTTP $codigo) — ABORTANDO."
    echo "     Migrar a nuvem sem ponto de retorno nao e' aceitavel."
    rm -f "$BACKUP"; exit 1
  fi
  ok "backup em $(basename "$BACKUP") ($(wc -c < "$BACKUP") bytes)"
else
  todo "exportar o realm para backup-$REALM-<data>.json antes de qualquer escrita"
fi

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
if [[ -z "$PM_ID" ]]; then echo "   ✘ client people-management-api não encontrado"; exit 1; fi
PM_ROLES="$(get "/clients/$PM_ID/roles")"

rename_client_role() { # <uuid> <de> <para> <descrição> <json dos papéis atuais>
  local cid="$1" from="$2" to="$3" desc="$4" roles="$5"
  if grep -q "\"name\" *: *\"$to\"" <<<"$roles"; then skip "$to já existe"; return 0; fi
  if grep -q "\"name\" *: *\"$from\"" <<<"$roles"; then
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
  if grep -q "\"name\" *: *\"$name\"" <<<"$PM_ROLES"; then skip "$name já existe"; else
    do_or_report "criar $name" POST "/clients/$PM_ID/roles" "{\"name\":\"$name\",\"description\":\"$desc\"}"
  fi
done

# ────────────────────────────────── 3. papéis de alçada do BillPayment (compostos)
step "3. Papéis de alçada do bill-payment-api"
BP_ID="$(client_uuid bill-payment-api)"
if [[ -z "$BP_ID" ]]; then echo "   ✘ client bill-payment-api não encontrado"; exit 1; fi
BP_ROLES="$(get "/clients/$BP_ID/roles")"

for pair in \
  "bill-approver-attention:Alcada para aprovar boleto de risco Atencao." \
  "bill-approver-danger:Alcada para aprovar boleto de risco Perigo. Cobre Atencao." \
  "bill-approver-extreme:Alcada para aprovar boleto de risco Extremo Perigo. Cobre Perigo e Atencao."
do
  name="${pair%%:*}"; desc="${pair#*:}"
  if grep -q "\"name\" *: *\"$name\"" <<<"$BP_ROLES"; then skip "$name já existe"; else
    do_or_report "criar $name" POST "/clients/$BP_ID/roles" "{\"name\":\"$name\",\"description\":\"$desc\"}"
  fi
done

# O composto é o que faz o console mostrar a cobertura real: quem tem -extreme
# aparece cobrindo -danger e -attention, sem ninguém precisar saber a hierarquia.
if [[ $APPLY -eq 1 ]]; then
  for pair in "bill-approver-danger:bill-approver-attention" "bill-approver-extreme:bill-approver-danger"; do
    parent="${pair%%:*}"; child="${pair#*:}"
    ja="$(get "/clients/$BP_ID/roles/$parent/composites" | python -c 'import json,sys; print(",".join(x["name"] for x in json.load(sys.stdin)))')"
    if grep -q "$child" <<<"$ja"; then skip "$parent já contém $child"; continue; fi
    parent_id="$(get "/clients/$BP_ID/roles/$parent" | python -c 'import json,sys;print(json.load(sys.stdin)["id"])')"
    child_repr="$(get "/clients/$BP_ID/roles/$child")"
    do_or_report "$parent passa a conter $child" POST "/roles-by-id/$parent_id/composites" "[$child_repr]"
  done
else
  todo "compor bill-approver-danger ⊃ attention, e extreme ⊃ danger"
fi

# ─────────────────────────────────────────── 4. configuração de autorização
step "4. Configuração de autorização (recursos, escopos, policies, permissões)"
echo "   O import MESCLA, não substitui — medido contra o Keycloak 26.3. O que existe no"
echo "   realm e não está no arquivo SOBREVIVE, e as policies antigas continuam concedendo"
echo "   (os papéis foram renomeados, e o id interno delas ainda resolve). Por isso a"
echo "   limpeza vem logo depois do import, e não antes."
import_authz "$PM_ID" "$PM_AUTHZ" people-management-api
import_authz "$BP_ID" "$BP_AUTHZ" bill-payment-api

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
  echo "   ⚠ O username da service account continua 'service-account-zapsign' — o Keycloak não o"
  echo "     renomeia. É cosmético: o vínculo é pelo UUID do client, e segue válido."
else
  skip "nem zapsign nem document-signing-client existem"
fi

# ─────────────────────────────────────────────────── 6. client scopes
step "6. Client scopes"
SCOPES="$(get "/client-scopes")"
scope_id() { python -c "
import json,sys
for s in json.load(sys.stdin):
    if s['name']=='$1': print(s['id']); break" <<<"$SCOPES"; }

rename_scope() { # <de> <para> <descrição>
  local from="$1" to="$2" desc="$3"
  if grep -q "\"name\" *: *\"$to\"" <<<"$SCOPES"; then skip "$to já existe"; return 0; fi
  local id; id="$(scope_id "$from")"
  if [[ -z "$id" ]]; then skip "$from não existe"; return 0; fi
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
echo "   papéis PM:   $(get "/clients/$PM_ID/roles" | python -c 'import json,sys;print(", ".join(sorted(r["name"] for r in json.load(sys.stdin))))')"
echo "   papéis BP:   $(get "/clients/$BP_ID/roles" | python -c 'import json,sys;print(", ".join(sorted(r["name"] for r in json.load(sys.stdin))))')"
echo "   realm roles: $(get "/roles" | python -c 'import json,sys;print(", ".join(sorted(r["name"] for r in json.load(sys.stdin))))')"
echo "   recursos PM: $(get "/clients/$PM_ID/authz/resource-server/resource?max=200" | python -c 'import json,sys;print(", ".join(sorted(r["name"] for r in json.load(sys.stdin))))')"
echo "   policies BP: $(get "/clients/$BP_ID/authz/resource-server/policy?max=200" | python -c 'import json,sys;print(len(json.load(sys.stdin)))') no total"

echo
echo "── Falta você fazer, e nenhum script deveria fazer sozinho ──────────"
echo "  a) Atribuir os papéis. Renomear preserva a atribuição antiga (quem era"
echo "     'admin' agora é 'people-admin'), mas os papéis NOVOS não se atribuem"
echo "     sozinhos — em especial as três alçadas de risco e o realm role 'developer'."
echo "  b) bill-admin NÃO aprova mais. Quem precisa aprovar recebe bill-approver"
echo "     explicitamente, e a alçada pelo papel do nível."
echo "  c) Conferir logando com cada usuário: um boleto de risco Atenção só é"
echo "     aprovável por quem tem bill-approver-attention ou acima."
