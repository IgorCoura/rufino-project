#!/usr/bin/env python
"""Confere o arquivo de realm ANTES de o Keycloak tentar importa-lo.

Existe porque o import e' tudo-ou-nada: uma referencia quebrada nao rejeita
aquele registro, ela DERRUBA O ARRANQUE do Keycloak inteiro. E a mensagem
raramente diz onde esta o problema — "Unable to find client specified for
service account link" nao nomeia o usuario, e o erro de tamanho de coluna nao
nomeia o campo.

Em 2026-09-04 tres importacoes seguidas falharam, uma por vez, cada uma por um
defeito diferente: descricao acima de 255, papel renomeado sem acertar o vinculo
do usuario, e o serviceAccountClientId apontando para o nome antigo do client.
Este script acha os tres de uma vez.

    python validate-realm.py [caminho-do-realm.json]
"""
import json
import io
import sys

LIMITE_COLUNA = 255
CAMPOS_COM_LIMITE = ("description", "displayName", "name")


def carregar(caminho):
    return json.load(io.open(caminho, encoding="utf-8-sig"))


def percorrer(node, path=""):
    if isinstance(node, dict):
        for k, v in node.items():
            yield f"{path}/{k}", k, v
            yield from percorrer(v, f"{path}/{k}")
    elif isinstance(node, list):
        for i, v in enumerate(node):
            yield from percorrer(v, f"{path}[{i}]")


def validar(d):
    erros = []
    avisos = []

    def erro(categoria, detalhe):
        erros.append(f"[{categoria}] {detalhe}")

    def aviso(categoria, detalhe):
        avisos.append(f"[{categoria}] {detalhe}")

    # ---------------------------------------------------------- inventarios
    clients = {c["clientId"]: c for c in d.get("clients", [])}
    realm_roles = {r["name"] for r in d.get("roles", {}).get("realm", [])}
    client_roles = {c: {r["name"] for r in rs}
                    for c, rs in d.get("roles", {}).get("client", {}).items()}
    client_scopes = {s["name"] for s in d.get("clientScopes", [])}

    # ------------------------------------------- 1. tamanho de coluna (255)
    for caminho, chave, valor in percorrer(d):
        if chave in CAMPOS_COM_LIMITE and isinstance(valor, str) and len(valor) > LIMITE_COLUNA:
            erro("tamanho", f"{caminho}: {len(valor)} caracteres (limite {LIMITE_COLUNA})")

    # ------------------------------------------------------- 2. usuarios
    for u in d.get("users", []):
        nome = u.get("username", "?")

        alvo = u.get("serviceAccountClientId")
        if alvo and alvo not in clients:
            erro("service account", f"usuario '{nome}' aponta para o client '{alvo}', que nao existe")

        for papel in u.get("realmRoles", []):
            if papel not in realm_roles:
                erro("papel de realm", f"usuario '{nome}' -> '{papel}' nao existe")

        for client, papeis in (u.get("clientRoles") or {}).items():
            if client not in clients:
                erro("papel de client", f"usuario '{nome}' -> client '{client}' nao existe")
                continue
            for papel in papeis:
                if papel not in client_roles.get(client, set()):
                    erro("papel de client", f"usuario '{nome}' -> '{client}/{papel}' nao existe")

    # ---------------------------------------------- 3. papeis compostos
    for client, papeis in d.get("roles", {}).get("client", {}).items():
        for papel in papeis:
            comp = (papel.get("composites") or {}).get("client") or {}
            for alvo_client, alvos in comp.items():
                for alvo in alvos:
                    if alvo not in client_roles.get(alvo_client, set()):
                        erro("composto", f"'{client}/{papel['name']}' contem '{alvo_client}/{alvo}', que nao existe")

    # --------------------------------------------- 4. client scopes ligados
    for c in d.get("clients", []):
        for chave in ("defaultClientScopes", "optionalClientScopes"):
            for s in c.get(chave, []):
                if s not in client_scopes:
                    erro("client scope", f"client '{c['clientId']}'.{chave} -> '{s}' nao existe")

    for chave in ("defaultDefaultClientScopes", "defaultOptionalClientScopes"):
        for s in d.get(chave, []):
            if s not in client_scopes:
                erro("client scope", f"realm.{chave} -> '{s}' nao existe")

    # ------------------------------------------------- 5. autorizacao (UMA)
    for c in d.get("clients", []):
        authz = c.get("authorizationSettings")
        if not authz:
            continue

        rotulo = c["clientId"]
        recursos = {r["name"]: {s["name"] for s in r.get("scopes", [])} for r in authz.get("resources", [])}
        escopos_declarados = {s["name"] for s in authz.get("scopes", [])}
        policies = {p["name"] for p in authz.get("policies", [])}

        for r in authz.get("resources", []):
            for s in r.get("scopes", []):
                if s["name"] not in escopos_declarados:
                    erro("escopo", f"{rotulo}: recurso '{r['name']}' usa o escopo '{s['name']}', ausente da lista 'scopes'")

        for p in authz.get("policies", []):
            cfg = p.get("config") or {}

            for bruto in json.loads(cfg.get("roles", "[]")) if cfg.get("roles") else []:
                ident = bruto["id"] if isinstance(bruto, dict) else bruto
                dono, _, papel = ident.partition("/")
                if papel:
                    if papel not in client_roles.get(dono, set()):
                        erro("policy", f"{rotulo}: policy '{p['name']}' cita '{ident}', que nao existe")
                elif dono not in realm_roles:
                    erro("policy", f"{rotulo}: policy '{p['name']}' cita o realm role '{dono}', que nao existe")

            for ref in json.loads(cfg.get("applyPolicies", "[]")) if cfg.get("applyPolicies") else []:
                if ref not in policies:
                    erro("policy", f"{rotulo}: permissao '{p['name']}' aplica '{ref}', que nao existe")

            if p.get("type") == "scope":
                for r in json.loads(cfg.get("resources", "[]")) if cfg.get("resources") else []:
                    if r not in recursos:
                        erro("permissao", f"{rotulo}: '{p['name']}' cita o recurso '{r}', que nao existe")
                        continue
                    for s in json.loads(cfg.get("scopes", "[]")) if cfg.get("scopes") else []:
                        if s not in recursos[r]:
                            # O Keycloak ACEITA isso: permissao de escopo com varios recursos e
                            # varios escopos concede a INTERSECAO. E' ilegivel no console, nao
                            # invalido — por isso e' aviso, e nao erro que bloqueia o import.
                            aviso("permissao", f"{rotulo}: '{p['name']}' cita '{r}#{s}', e o recurso nao declara esse escopo")

    # ------------------------------------------ 6. mappers de audience
    for cs in d.get("clientScopes", []):
        for m in cs.get("protocolMappers", []):
            alvo = (m.get("config") or {}).get("included.client.audience")
            if alvo and alvo not in clients:
                erro("audience", f"client scope '{cs['name']}', mapper '{m['name']}' -> audience '{alvo}', que nao e' um client deste realm")

    # ------------------------------------------------- 7. user profile
    componentes = d.get("components", {}).get("org.keycloak.userprofile.UserProfileProvider", [])
    for comp in componentes:
        bruto = (comp.get("config") or {}).get("kc.user.profile.config", [None])[0]
        if not bruto:
            continue
        perfil = json.loads(bruto)

        politica = perfil.get("unmanagedAttributePolicy")
        if politica is not None and politica not in ("ENABLED", "ADMIN_VIEW", "ADMIN_EDIT"):
            erro("user profile", f"unmanagedAttributePolicy='{politica}' e' invalido — os valores sao "
                                 "ENABLED/ADMIN_VIEW/ADMIN_EDIT, e a AUSENCIA significa 'descarta o nao declarado'")

        declarados = {a["name"] for a in perfil.get("attributes", [])}

        # Os client scopes EMBUTIDOS do Keycloak (profile, phone, email...) mapeiam atributos
        # padrao do OIDC que o User Profile nao declara e nao precisa declarar. O check so vale
        # para os scopes que NOS criamos — e' la que um atributo nao declarado vira claim vazio.
        EMBUTIDOS = {"profile", "phone", "email", "address", "roles", "web-origins", "basic",
                     "acr", "microprofile-jwt", "offline_access", "organization", "role_list",
                     "saml_organization", "service_account"}

        for cs in d.get("clientScopes", []):
            if cs["name"] in EMBUTIDOS:
                continue
            for m in cs.get("protocolMappers", []):
                cfg = m.get("config") or {}
                if m.get("protocolMapper") == "oidc-usermodel-attribute-mapper":
                    atributo = cfg.get("user.attribute")
                    if atributo and atributo not in declarados:
                        erro("user profile", f"mapper '{cs['name']}/{m['name']}' le o atributo '{atributo}', "
                                             "que o User Profile NAO declara — o valor e' descartado na escrita "
                                             "com HTTP 204 e o claim nunca aparece no token")

    return erros, avisos


def main():
    caminho = sys.argv[1] if len(sys.argv) > 1 else "RufinoRealm/realm-import-2026-08-18.json"
    erros, avisos = validar(carregar(caminho))

    print(f"=== {caminho}")

    if avisos:
        print(f"    {len(avisos)} aviso(s) — nao bloqueiam o import:")
        for a in avisos:
            print("    ~", a)
        print()

    if not erros:
        print("    OK — nenhuma referencia quebrada, nenhum campo acima do limite de coluna.")
        return 0

    print(f"    {len(erros)} ERRO(S) — o import falha e o Keycloak NAO sobe:")
    print()
    for e in erros:
        print("   ", e)
    return 1


if __name__ == "__main__":
    sys.exit(main())
