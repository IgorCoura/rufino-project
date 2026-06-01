# Política de Privacidade — Rufino

**Última atualização:** 1 de junho de 2026
**Versão do aplicativo a que se aplica:** 2.0.0 e posteriores


---

## 1. Quem somos

O **Rufino** ("aplicativo", "app", "nós") é um aplicativo de **gestão de
pessoas e documentos de recursos humanos** desenvolvido e operado por
**IGOR DE BRITO COURA LTDA**, inscrita no CNPJ sob o nº
** 55.171.596/0001-84 **, com sede em ** Rua Justino Paixão, 555, apto 124, Jardim São Caetano, São Caetano do Sul - SP **.

O Rufino destina-se ao uso **profissional por empresas** (modelo B2B). Ele
permite que departamentos de RH cadastrem, organizem e gerenciem funcionários,
departamentos, locais de trabalho, documentos, exames e fluxos de assinatura
eletrônica.

Esta Política descreve como tratamos dados pessoais em conformidade com a
**Lei nº 13.709/2018 — Lei Geral de Proteção de Dados Pessoais (LGPD)** e com
as exigências da **Google Play** (Seção de Segurança dos Dados / *Data
Safety*).

---

## 2. Definições (conforme a LGPD)

- **Dado pessoal:** informação relacionada a pessoa natural identificada ou
  identificável (ex.: nome, CPF, e-mail).
- **Dado pessoal sensível:** dado sobre origem racial ou étnica, convicção
  religiosa, opinião política, saúde, vida sexual, dado genético ou biométrico,
  entre outros (art. 5º, II da LGPD).
- **Titular:** a pessoa natural a quem os dados se referem.
- **Controlador:** quem toma as decisões sobre o tratamento dos dados.
- **Operador:** quem trata os dados em nome do controlador.
- **Tratamento:** toda operação com dados (coleta, uso, armazenamento,
  compartilhamento, eliminação etc.).
- **Encarregado (DPO):** pessoa indicada para atuar como canal de comunicação
  entre o controlador, os titulares e a ANPD.

---

## 3. Papéis no tratamento de dados — quem é o controlador

Como o Rufino é uma ferramenta corporativa, é importante distinguir dois
cenários:

### 3.1. Dados dos funcionários cadastrados (titulares finais)

Quando uma empresa-cliente usa o Rufino para gerir seus funcionários, **a
empresa-cliente (empregadora) é a CONTROLADORA** desses dados — é ela quem
decide quais dados inserir e para qual finalidade. A **CouraTech atua como
OPERADORA**, tratando os dados estritamente conforme as instruções e o contrato
firmado com a empresa-cliente.

> Se você é funcionário de uma empresa que usa o Rufino e deseja exercer seus
> direitos sobre seus dados (acesso, correção, exclusão etc.), o canal primário
> é o **departamento de RH da sua empregadora**, que é a controladora. A
> CouraTech apoiará a empresa no atendimento dessas solicitações.

### 3.2. Dados da conta de quem usa o app (usuário do RH)

Em relação aos dados de **login e uso do aplicativo** pelo profissional de RH
que acessa o app (credenciais, identificador da conta, registros técnicos), a
**CouraTech atua como CONTROLADORA**, pois decide sobre esse tratamento para
viabilizar a autenticação, a segurança e o funcionamento do serviço.

---

## 4. Quais dados coletamos

O Rufino trata as seguintes categorias de dados. Note que **a maior parte dos
dados é inserida pelo próprio profissional de RH** ao cadastrar funcionários —
o app é a ferramenta, e o conteúdo é definido pela empresa-cliente.

### 4.1. Dados de cadastro e identificação de funcionários

- Nome completo, nome social, data de nascimento;
- Filiação (nome da mãe e do pai);
- **CPF, RG, CNPJ, PIS/PASEP, Título de Eleitor, Certificado/Documento Militar**;
- Carteira de identidade (órgão emissor, data de emissão, UF).

### 4.2. Dados de contato e endereço

- E-mail, telefone, celular;
- Endereço completo (CEP, logradouro, número, complemento, bairro, cidade, UF) —
  o CEP pode ser consultado automaticamente via serviço público de busca (ViaCEP).

### 4.3. Dados contratuais e de trabalho

- Cargo, função, departamento, local de trabalho;
- Dados de contrato de trabalho;
- **Remuneração / salário** (dado de natureza confidencial).

### 4.4. Dados sensíveis (art. 5º, II da LGPD)

- **Dados de exames médicos ocupacionais** (informação de saúde).

Dados sensíveis recebem proteção reforçada e são tratados apenas para a
finalidade de gestão de pessoas instruída pela empresa-cliente, com base legal
adequada (ver Seção 6).

### 4.5. Dados de dependentes

- Nome, data de nascimento, parentesco e documentos de dependentes do
  funcionário, quando necessários para benefícios e obrigações trabalhistas.

### 4.6. Documentos e imagens

- Documentos digitalizados (PDF e imagens) capturados pela câmera ou enviados
  pelo dispositivo;
- Modelos de documentos, grupos de documentos e documentos exigidos;
- Dados de fluxo de **assinatura eletrônica** de documentos.

### 4.7. Dados de conta e autenticação

- Credenciais de acesso e tokens de autenticação (gerenciados via Keycloak/OAuth2);
- Identificadores da empresa e permissões do usuário.

### 4.8. Dados técnicos e de diagnóstico

- **Relatórios de erro e travamento (crash)**, com dados técnicos do
  dispositivo, para diagnóstico e correção de falhas. Antes do envio,
  aplicamos um mecanismo de **anonimização/ocultação (PII scrubbing)** que
  remove dados pessoais sensíveis conhecidos (CPF, nome, salário, e-mail,
  tokens etc.) dos relatórios.

### 4.9. Dados que NÃO coletamos

- O Rufino **não** coleta dados de **localização (GPS)**;
- **Não** exibe anúncios nem usa identificadores de publicidade;
- **Não** vende dados pessoais a terceiros;
- O reconhecimento de texto (OCR) dos documentos digitalizados ocorre
  **localmente no dispositivo** — as imagens não são enviadas a terceiros para
  esse processamento.

---

## 5. Como e quando coletamos os dados

- **Inserção manual** pelo profissional de RH durante o cadastro;
- **Câmera do dispositivo**, ao digitalizar documentos (mediante sua permissão);
- **Importação de arquivos** do dispositivo (seletor de arquivos);
- **Geração automática**, no caso de tokens de autenticação, identificadores de
  requisição e relatórios técnicos de erro;
- **Serviços de apoio**, como a consulta de CEP em base pública.

---

## 6. Finalidades e bases legais (art. 7º e art. 11 da LGPD)

| Finalidade | Base legal |
|---|---|
| Gerir o cadastro de funcionários e documentos de RH | Execução de contrato com a empresa-cliente; cumprimento de obrigação legal/regulatória trabalhista (art. 7º, V e II) |
| Tratar dados sensíveis (exames médicos) para gestão ocupacional | Cumprimento de obrigação legal/regulatória; ou tutela da saúde (art. 11, II, "a"/"f") |
| Autenticar e controlar o acesso ao aplicativo | Execução de contrato; legítimo interesse na segurança (art. 7º, V e IX) |
| Garantir a segurança da informação e prevenir fraudes | Legítimo interesse; cumprimento de obrigação legal (art. 7º, IX e II) |
| Diagnosticar e corrigir falhas técnicas (crash reports) | Legítimo interesse na melhoria e estabilidade do serviço (art. 7º, IX) |
| Operar fluxos de assinatura eletrônica de documentos | Execução de contrato; cumprimento de obrigação legal (art. 7º, V e II) |

A base legal específica aplicável aos dados dos funcionários é definida pela
**empresa-cliente, na qualidade de controladora**.

---

## 7. Compartilhamento de dados

Não vendemos dados pessoais. Compartilhamos dados apenas com:

- **A empresa-cliente (controladora)** e seus usuários autorizados, que acessam
  os dados que ela própria inseriu;
- **Provedores de infraestrutura e serviços (operadores/suboperadores)**
  estritamente necessários ao funcionamento do app, sob contrato e dever de
  confidencialidade:
  - **Provedor de nuvem / hospedagem do back-end** (ex.: Microsoft Azure) —
    armazenamento e processamento dos dados;
  - **Keycloak** — serviço de identidade e autenticação (gestão de credenciais
    e permissões);
  - **Serviço de monitoramento de erros** (ex.: Sentry) — recebe relatórios
    técnicos de falha já submetidos à ocultação de dados pessoais;
  - **Serviço de assinatura eletrônica** — quando o fluxo de assinatura de
    documentos é utilizado;
  - **ViaCEP** (serviço público) — apenas o CEP é enviado para retorno do
    endereço; nenhum dado pessoal identificável é transmitido nessa consulta.
- **Autoridades públicas**, quando exigido por lei, ordem judicial ou
  requisição de autoridade competente.

---

## 8. Permissões do dispositivo

O Rufino solicita as seguintes permissões, sempre com finalidade específica:

| Permissão | Para quê | É opcional? |
|---|---|---|
| **Câmera** | Digitalizar documentos de funcionários | Sim — só é solicitada quando você usa o scanner; o app funciona sem ela |
| **Internet / Rede** | Comunicação com o servidor para sincronizar dados | Necessária para o funcionamento |
| **Acesso a arquivos** | Selecionar e salvar documentos (PDF/planilhas) | Solicitada apenas quando você escolhe ou salva um arquivo |

Você pode revogar permissões a qualquer momento nas configurações do sistema
operacional do dispositivo.

---

## 9. Armazenamento, segurança e transferência internacional

Adotamos medidas técnicas e administrativas para proteger os dados (art. 46 da
LGPD), incluindo:

- **Criptografia** de credenciais e tokens em repouso no dispositivo
  (armazenamento seguro do sistema operacional);
- **Comunicação criptografada** (HTTPS/TLS) entre o app e o servidor;
- **Controle de acesso granular por permissões** — cada usuário só enxerga e
  executa o que sua função autoriza;
- **Ocultação de dados pessoais (PII scrubbing)** nos relatórios técnicos antes
  do envio;
- Princípio do menor privilégio e segregação por empresa (multi-tenant).

**Transferência internacional:** alguns provedores podem armazenar ou processar
dados em servidores localizados **fora do Brasil**. Nesses casos, garantimos
que a transferência observe os requisitos dos arts. 33 a 36 da LGPD (país com
nível adequado de proteção ou cláusulas/garantias contratuais apropriadas).

---

## 10. Retenção e eliminação dos dados

Os dados são mantidos pelo tempo necessário ao cumprimento das finalidades e das
obrigações legais (especialmente as **trabalhistas e previdenciárias**, que
podem exigir guarda por prazos legais determinados).

Para os dados em que a CouraTech atua como **operadora**, o prazo de retenção e
a eliminação seguem as instruções da **empresa-cliente controladora**.
Encerrado o prazo ou a relação contratual, os dados são eliminados ou
anonimizados, salvo hipóteses de guarda obrigatória previstas em lei (art. 16
da LGPD).

---

## 11. Direitos do titular (art. 18 da LGPD)

Você, como titular, pode a qualquer momento solicitar:

- **Confirmação** da existência de tratamento;
- **Acesso** aos seus dados;
- **Correção** de dados incompletos, inexatos ou desatualizados;
- **Anonimização, bloqueio ou eliminação** de dados desnecessários ou tratados
  em desconformidade com a lei;
- **Portabilidade** a outro fornecedor de serviço;
- **Eliminação** dos dados tratados com base no consentimento;
- **Informação** sobre as entidades com as quais compartilhamos dados;
- **Informação** sobre a possibilidade de não consentir e suas consequências;
- **Revogação do consentimento**, quando aplicável.

**Como exercer:** se você é funcionário cadastrado, contate o **RH da sua
empregadora** (controladora). Para dados em que a CouraTech é controladora, ou
para dúvidas sobre esta Política, use os canais da Seção 14.

---

## 12. Dados de crianças e adolescentes

O Rufino é uma ferramenta corporativa destinada a profissionais de RH e **não se
dirige a crianças**. Eventuais dados de dependentes menores de idade são
inseridos pela empresa-cliente para fins trabalhistas/benefícios e tratados no
melhor interesse do titular, conforme o art. 14 da LGPD.

---

## 13. Alterações nesta Política

Esta Política pode ser atualizada para refletir mudanças legais ou no serviço. A
data da "Última atualização" no topo indica a versão vigente. Alterações
relevantes serão comunicadas pelos canais apropriados. O uso continuado do app
após a atualização significa ciência da versão vigente.

---

## 14. Contato e Encarregado (DPO)

Para exercer direitos, esclarecer dúvidas ou tratar de assuntos relacionados à
proteção de dados:

- **Controlador:** IGOR DE BRITO COURA LTDA — CNPJ 55.171.596/0001-84
- **Encarregado pelo Tratamento de Dados (DPO):** Igor de Brito Coura
- **E-mail:** igor-coura@hotmail.com
- **Endereço:** Rua Justino Paixão, 555, apto 124, Jardim São Caetano, São Caetano do Sul - SP

Você também pode apresentar reclamação à **Autoridade Nacional de Proteção de
Dados (ANPD)** — https://www.gov.br/anpd.

---

*Esta Política de Privacidade foi elaborada para atender à Lei nº 13.709/2018
(LGPD) e às exigências da Google Play sobre privacidade e segurança de dados.*
