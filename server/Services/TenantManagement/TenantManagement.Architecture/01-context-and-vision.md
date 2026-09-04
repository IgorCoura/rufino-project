# 01 — Contexto e visão

## O problema que este BC resolve

A plataforma tem dois produtos — gestão de pessoas e contas a pagar — e **ninguém emitia a
identidade do cliente**. O `TenantId` existia de fato (viaja no claim `companies` do token e no
`{tenantId}` da rota do BillPayment), mas nascia à mão: alguém criava a `Company` pelo
PeopleManagement, e alguém preenchia o atributo do usuário no console do Keycloak.

Isso funcionava enquanto todo cliente era pessoa jurídica. **O BillPayment atende pessoa física
também**, e `Company` é CNPJ-only por construção — está no nome, no VO e na tabela. Não havia por
onde cadastrar uma pessoa física como cliente da plataforma.

## O que este BC é

**Emitir a identidade do cliente e dizer quem a acessa.** Nada além disso.

- Cadastra **pessoa física e jurídica no mesmo modelo**. A diferença mora em três lugares: o tipo
  de documento, o direito a nome fantasia e o tipo de subconta no provedor de pagamento (que é
  assunto do BillPayment). Em nenhum outro.
- Emite o `TenantId` que os produtos usam na rota e no token.
- Registra quem tem acesso a qual tenant, e leva essa concessão até o provedor de identidade.
- Registra quais produtos o tenant tem habilitados.

## O que este BC NÃO é

- **Não substitui o `Company` do PeopleManagement nem o `PayerProfile` do BillPayment.** Cada
  produto continua dono do seu cadastro local — ver [ADR-002](adr/ADR-002-registro-mestre.md).
- **Não é servidor de autorização.** Papéis, escopos e políticas vivem no Keycloak. Este BC é
  resource server como qualquer outro.
- **Não é chamado em tempo de execução pelos produtos.** A única coisa que atravessa é o claim do
  token. Se este serviço cair, ninguém deixa de pagar boleto nem de emitir documento.
- **Não faz autosserviço.** O cadastro é do back-office. Signup público exigiria verificação de
  e-mail, proteção anti-abuso e confirmação de documento — escopo próprio, decidido depois.

## Linguagem ubíqua

| Termo | Significado |
|---|---|
| **Tenant** | O cliente da plataforma. Pessoa física ou jurídica. Unidade de isolamento e de dinheiro. |
| **Vínculo** (`TenantMembership`) | O acesso de uma pessoa a um tenant. Chaveado por **e-mail**, porque a pessoa pode ainda não existir no provedor de identidade quando o acesso é concedido. |
| **Responsável** (`Owner`) | Quem responde pelo tenant. Todo tenant tem ao menos um, sempre. |
| **Produto habilitado** | Um dos produtos da plataforma ligado para aquele tenant. Desligar não apaga: vira histórico. |
| **Provisionamento** | Levar a concessão de acesso até o provedor de identidade. É o único passo do BC que não é transacional. |
| **Suspensão** | Cadastro preservado, alterações bloqueadas. Não apaga nada e não libera o documento. |

## Premissas

1. **Um documento fiscal, um tenant.** CPF e CNPJ do mesmo dono são tenants diferentes — é assim
   que a alçada e a subconta de pagamento ficam separadas, e é o caso do MEI.
2. **Cadastro sem dono não existe.** Cadastrar e conceder o acesso do titular é uma operação só.
3. **Endereço é obrigatório.** A subconta no provedor de pagamento exige endereço para PF e PJ;
   deixá-lo opcional adiaria a descoberta para o dia em que o dinheiro precisa andar.
4. **O Id pode ser informado.** É o que permite migrar um cadastro que já tem identidade em outro
   lugar sem reemitir o acesso de ninguém.
