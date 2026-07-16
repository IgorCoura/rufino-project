-- =====================================================================
-- CLONE_COMPANY_PM.SQL
-- Clona TODOS os dados de uma empresa (source) para uma NOVA empresa,
-- gerando novos GUIDs e remapeando todas as FKs / referencias logicas.
--
-- USO (SEMPRE numa COPIA/DUMP do banco, nunca em producao):
--   psql "postgresql://user:pass@host:5432/rufino_copia" \
--        -v src_company="'<GUID_DA_EMPRESA_ORIGEM>'" \
--        -v new_cnpj="'00000000000191'" \
--        -v new_corporate="'EMPRESA CLONE LTDA'" \
--        -v new_fantasy="'Empresa Clone'" \
--        -f CLONE_COMPANY_PM.SQL
--
-- O script roda dentro de uma transacao. Para testar sem gravar,
-- troque o COMMIT do final por ROLLBACK.
--
-- DocumentsUnits: NAO clona o historico. Para cada Document recria
-- APENAS o ULTIMO DocumentUnit (mais recente por Date, depois Id) no
-- estado "recem-criado" (Pending, sem arquivo/assinatura), preservando
-- o Period e a Date. Reflete Document.NewDocumentUnit no dominio.
--
-- NAO copia blobs do S3 (Garage.io) E zera TODAS as referencias a objetos
-- do S3 no clone (para nao apontar p/ arquivos da origem):
--   * Employees._image_FileName / _image_Extension (foto)  -> NULL
--   * DocumentTemplates.TemplateFileInfo_(Directory/Header/Body/Footer) -> NULL
--   * DocumentsUnits: recriados Pending, sem Name/Extension/Signature -> NULL
--
-- MARCACAO: toda coluna chamada "Name" nas tabelas clonadas recebe o
-- prefixo 'T - ' no conteudo, para identificar os dados de teste.
-- (DocumentsUnits.Name fica NULL; Companies usa CorporateName/FantasyName
-- via parametros, nao a coluna "Name".)
--
-- Excluidas do clone (nao sao dados de tenant): WebHooks, ClientRequests.
-- Excluidas por decisao (nao finalizadas): Archives, ArchiveCategories, File.
-- =====================================================================

BEGIN;

SET search_path TO people_management;

-- ---------------------------------------------------------------------
-- 0. Mapa da empresa (raiz). Uma unica linha: old_id -> new_id.
-- ---------------------------------------------------------------------
CREATE TEMP TABLE map_company ON COMMIT DROP AS
SELECT "Id" AS old_id, gen_random_uuid() AS new_id
FROM "Companies"
WHERE "Id" = :src_company;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM map_company) THEN
    RAISE EXCEPTION 'Empresa origem nao encontrada (verifique o parametro src_company)';
  END IF;
END $$;

-- Stage + insert da propria empresa (troca Id, Cnpj, nomes).
CREATE TEMP TABLE stg_company ON COMMIT DROP AS
SELECT * FROM "Companies" WHERE "Id" = (SELECT old_id FROM map_company);

UPDATE stg_company SET
  "Id"            = (SELECT new_id FROM map_company),
  "Cnpj"          = :new_cnpj,
  "CorporateName" = :new_corporate,
  "FantasyName"   = :new_fantasy;

INSERT INTO "Companies" SELECT * FROM stg_company;

-- ---------------------------------------------------------------------
-- Helper mental: para cada tabela com PK uuid escopada por empresa:
--   1) map_X       = old_id -> gen_random_uuid()
--   2) stg_X       = SELECT * das linhas da empresa origem
--   3) UPDATE stg_X: Id novo, CompanyId novo, FKs remapeadas
--   4) INSERT INTO X SELECT * FROM stg_X
-- ---------------------------------------------------------------------

-- =========================== Departments =============================
CREATE TEMP TABLE map_department ON COMMIT DROP AS
SELECT "Id" AS old_id, gen_random_uuid() AS new_id
FROM "Departments" WHERE "CompanyId" = (SELECT old_id FROM map_company);

CREATE TEMP TABLE stg_department ON COMMIT DROP AS
SELECT * FROM "Departments" WHERE "CompanyId" = (SELECT old_id FROM map_company);
UPDATE stg_department s SET "Id" = m.new_id FROM map_department m WHERE s."Id" = m.old_id;
UPDATE stg_department SET "CompanyId" = (SELECT new_id FROM map_company);
UPDATE stg_department SET "Name" = 'T - ' || "Name";
INSERT INTO "Departments" SELECT * FROM stg_department;

-- =========================== Workplaces ==============================
CREATE TEMP TABLE map_workplace ON COMMIT DROP AS
SELECT "Id" AS old_id, gen_random_uuid() AS new_id
FROM "Workplaces" WHERE "CompanyId" = (SELECT old_id FROM map_company);

CREATE TEMP TABLE stg_workplace ON COMMIT DROP AS
SELECT * FROM "Workplaces" WHERE "CompanyId" = (SELECT old_id FROM map_company);
UPDATE stg_workplace s SET "Id" = m.new_id FROM map_workplace m WHERE s."Id" = m.old_id;
UPDATE stg_workplace SET "CompanyId" = (SELECT new_id FROM map_company);
UPDATE stg_workplace SET "Name" = 'T - ' || "Name";
INSERT INTO "Workplaces" SELECT * FROM stg_workplace;

-- =========================== Positions (-> Department) ===============
CREATE TEMP TABLE map_position ON COMMIT DROP AS
SELECT "Id" AS old_id, gen_random_uuid() AS new_id
FROM "Positions" WHERE "CompanyId" = (SELECT old_id FROM map_company);

CREATE TEMP TABLE stg_position ON COMMIT DROP AS
SELECT * FROM "Positions" WHERE "CompanyId" = (SELECT old_id FROM map_company);
UPDATE stg_position s SET "Id" = m.new_id FROM map_position m WHERE s."Id" = m.old_id;
UPDATE stg_position SET "CompanyId" = (SELECT new_id FROM map_company);
UPDATE stg_position s SET "DepartmentId" = m.new_id FROM map_department m WHERE s."DepartmentId" = m.old_id;
UPDATE stg_position SET "Name" = 'T - ' || "Name";
INSERT INTO "Positions" SELECT * FROM stg_position;

-- =========================== Roles (-> Position) =====================
CREATE TEMP TABLE map_role ON COMMIT DROP AS
SELECT "Id" AS old_id, gen_random_uuid() AS new_id
FROM "Roles" WHERE "CompanyId" = (SELECT old_id FROM map_company);

CREATE TEMP TABLE stg_role ON COMMIT DROP AS
SELECT * FROM "Roles" WHERE "CompanyId" = (SELECT old_id FROM map_company);
UPDATE stg_role s SET "Id" = m.new_id FROM map_role m WHERE s."Id" = m.old_id;
UPDATE stg_role SET "CompanyId" = (SELECT new_id FROM map_company);
UPDATE stg_role s SET "PositionId" = m.new_id FROM map_position m WHERE s."PositionId" = m.old_id;
UPDATE stg_role SET "Name" = 'T - ' || "Name";
INSERT INTO "Roles" SELECT * FROM stg_role;

-- =========================== DocumentGroups ==========================
CREATE TEMP TABLE map_documentgroup ON COMMIT DROP AS
SELECT "Id" AS old_id, gen_random_uuid() AS new_id
FROM "DocumentGroups" WHERE "CompanyId" = (SELECT old_id FROM map_company);

CREATE TEMP TABLE stg_documentgroup ON COMMIT DROP AS
SELECT * FROM "DocumentGroups" WHERE "CompanyId" = (SELECT old_id FROM map_company);
UPDATE stg_documentgroup s SET "Id" = m.new_id FROM map_documentgroup m WHERE s."Id" = m.old_id;
UPDATE stg_documentgroup SET "CompanyId" = (SELECT new_id FROM map_company);
UPDATE stg_documentgroup SET "Name" = 'T - ' || "Name";
INSERT INTO "DocumentGroups" SELECT * FROM stg_documentgroup;

-- =========================== DocumentTemplates (-> DocumentGroup) ====
CREATE TEMP TABLE map_documenttemplate ON COMMIT DROP AS
SELECT "Id" AS old_id, gen_random_uuid() AS new_id
FROM "DocumentTemplates" WHERE "CompanyId" = (SELECT old_id FROM map_company);

CREATE TEMP TABLE stg_documenttemplate ON COMMIT DROP AS
SELECT * FROM "DocumentTemplates" WHERE "CompanyId" = (SELECT old_id FROM map_company);
UPDATE stg_documenttemplate s SET "Id" = m.new_id FROM map_documenttemplate m WHERE s."Id" = m.old_id;
UPDATE stg_documenttemplate SET "CompanyId" = (SELECT new_id FROM map_company);
UPDATE stg_documenttemplate s SET "DocumentGroupId" = m.new_id FROM map_documentgroup m WHERE s."DocumentGroupId" = m.old_id;
UPDATE stg_documenttemplate SET "Name" = 'T - ' || "Name";
-- Zera referencias a arquivos no S3 (o clone nao aponta p/ objetos da origem).
UPDATE stg_documenttemplate SET
  "TemplateFileInfo_Directory"      = NULL,
  "TemplateFileInfo_HeaderFileName" = NULL,
  "TemplateFileInfo_BodyFileName"   = NULL,
  "TemplateFileInfo_FooterFileName" = NULL;
INSERT INTO "DocumentTemplates" SELECT * FROM stg_documenttemplate;

--   PlaceSignature (filha owned de DocumentTemplate; PK composta, Id int mantido)
CREATE TEMP TABLE stg_placesignature ON COMMIT DROP AS
SELECT * FROM "PlaceSignature"
WHERE "DocumentTemplateId" IN (SELECT old_id FROM map_documenttemplate);
UPDATE stg_placesignature s SET "DocumentTemplateId" = m.new_id
FROM map_documenttemplate m WHERE s."DocumentTemplateId" = m.old_id;
INSERT INTO "PlaceSignature" SELECT * FROM stg_placesignature;

-- =========================== Employees (-> Role, Workplace) ==========
CREATE TEMP TABLE map_employee ON COMMIT DROP AS
SELECT "Id" AS old_id, gen_random_uuid() AS new_id
FROM "Employees" WHERE "CompanyId" = (SELECT old_id FROM map_company);

CREATE TEMP TABLE stg_employee ON COMMIT DROP AS
SELECT * FROM "Employees" WHERE "CompanyId" = (SELECT old_id FROM map_company);
UPDATE stg_employee s SET "Id" = m.new_id FROM map_employee m WHERE s."Id" = m.old_id;
UPDATE stg_employee SET "CompanyId" = (SELECT new_id FROM map_company);
UPDATE stg_employee s SET "RoleId"      = m.new_id FROM map_role      m WHERE s."RoleId" = m.old_id;
UPDATE stg_employee s SET "WorkPlaceId" = m.new_id FROM map_workplace m WHERE s."WorkPlaceId" = m.old_id;  -- casing: WorkPlaceId
-- Prefixo 'T ' (SEM '-'): o VO EmployeeAggregate.Name tem regex estrita
-- (^[letras'] ( [letras'])+$) que rejeita '-' e digitos. 'T - NOME' quebraria
-- a leitura da entidade. Idem para Dependent.Name (mesmo VO).
UPDATE stg_employee SET "Name" = 'T ' || "Name";
-- Zera referencia a arquivo no S3 (foto do funcionario).
UPDATE stg_employee SET "_image_FileName" = NULL, "_image_Extension" = NULL;
INSERT INTO "Employees" SELECT * FROM stg_employee;

--   Dependent (filha owned; PK composta (EmployeeId, Id) -> Id int mantido)
CREATE TEMP TABLE stg_dependent ON COMMIT DROP AS
SELECT * FROM "Dependent" WHERE "EmployeeId" IN (SELECT old_id FROM map_employee);
UPDATE stg_dependent s SET "EmployeeId" = m.new_id FROM map_employee m WHERE s."EmployeeId" = m.old_id;
UPDATE stg_dependent SET "Name" = 'T ' || "Name";  -- 'T ' sem '-' (regex estrita do Name)
INSERT INTO "Dependent" SELECT * FROM stg_dependent;

--   EmploymentContract (filha owned; PK composta -> Id int mantido)
CREATE TEMP TABLE stg_contract ON COMMIT DROP AS
SELECT * FROM "EmploymentContract" WHERE "EmployeeId" IN (SELECT old_id FROM map_employee);
UPDATE stg_contract s SET "EmployeeId" = m.new_id FROM map_employee m WHERE s."EmployeeId" = m.old_id;
INSERT INTO "EmploymentContract" SELECT * FROM stg_contract;

-- =========================== Mapa combinado de entidades =============
-- Usado para remapear RequireDocuments.AssociationIds[] (FK logica sem
-- constraint; aponta p/ Employee/Role/Position/Department conforme AssociationType).
CREATE TEMP TABLE map_entity ON COMMIT DROP AS
  SELECT old_id, new_id FROM map_employee
  UNION ALL SELECT old_id, new_id FROM map_role
  UNION ALL SELECT old_id, new_id FROM map_position
  UNION ALL SELECT old_id, new_id FROM map_department;

-- =========================== RequireDocuments ========================
CREATE TEMP TABLE map_requiredocuments ON COMMIT DROP AS
SELECT "Id" AS old_id, gen_random_uuid() AS new_id
FROM "RequireDocuments" WHERE "CompanyId" = (SELECT old_id FROM map_company);

CREATE TEMP TABLE stg_requiredocuments ON COMMIT DROP AS
SELECT * FROM "RequireDocuments" WHERE "CompanyId" = (SELECT old_id FROM map_company);
UPDATE stg_requiredocuments s SET "Id" = m.new_id FROM map_requiredocuments m WHERE s."Id" = m.old_id;
UPDATE stg_requiredocuments SET "CompanyId" = (SELECT new_id FROM map_company);

--   Remapeia arrays uuid[] elemento a elemento (mantem os que nao tem mapa).
-- COALESCE p/ o valor original preserva array vazio ('{}'): array_agg sobre
-- zero elementos retorna NULL, e a coluna e NOT NULL em producao.
UPDATE stg_requiredocuments s SET "DocumentsTemplatesIds" = COALESCE((
  SELECT array_agg(COALESCE(m.new_id, u.elem) ORDER BY u.ord)
  FROM unnest(s."DocumentsTemplatesIds") WITH ORDINALITY AS u(elem, ord)
  LEFT JOIN map_documenttemplate m ON m.old_id = u.elem
), s."DocumentsTemplatesIds")
WHERE s."DocumentsTemplatesIds" IS NOT NULL;

UPDATE stg_requiredocuments s SET "AssociationIds" = COALESCE((
  SELECT array_agg(COALESCE(m.new_id, u.elem) ORDER BY u.ord)
  FROM unnest(s."AssociationIds") WITH ORDINALITY AS u(elem, ord)
  LEFT JOIN map_entity m ON m.old_id = u.elem
), s."AssociationIds")
WHERE s."AssociationIds" IS NOT NULL;

UPDATE stg_requiredocuments SET "Name" = 'T - ' || "Name";
INSERT INTO "RequireDocuments" SELECT * FROM stg_requiredocuments;

--   ListenEvent (filha owned; PK composta -> Id int mantido)
CREATE TEMP TABLE stg_listenevent ON COMMIT DROP AS
SELECT * FROM "ListenEvent" WHERE "RequireDocumentsId" IN (SELECT old_id FROM map_requiredocuments);
UPDATE stg_listenevent s SET "RequireDocumentsId" = m.new_id
FROM map_requiredocuments m WHERE s."RequireDocumentsId" = m.old_id;
INSERT INTO "ListenEvent" SELECT * FROM stg_listenevent;

-- =========================== Join DocumentTemplateRequireDocuments ===
CREATE TEMP TABLE stg_dtrd ON COMMIT DROP AS
SELECT * FROM "DocumentTemplateRequireDocuments"
WHERE "RequireDocumentsId" IN (SELECT old_id FROM map_requiredocuments);
UPDATE stg_dtrd s SET "DocumentTemplateId" = m.new_id FROM map_documenttemplate m WHERE s."DocumentTemplateId" = m.old_id;
UPDATE stg_dtrd s SET "RequireDocumentsId" = m.new_id FROM map_requiredocuments m WHERE s."RequireDocumentsId" = m.old_id;
INSERT INTO "DocumentTemplateRequireDocuments" SELECT * FROM stg_dtrd;

-- =========================== Documents (-> Template, Employee, RequireDoc)
CREATE TEMP TABLE map_document ON COMMIT DROP AS
SELECT "Id" AS old_id, gen_random_uuid() AS new_id
FROM "Documents" WHERE "CompanyId" = (SELECT old_id FROM map_company);

CREATE TEMP TABLE stg_document ON COMMIT DROP AS
SELECT * FROM "Documents" WHERE "CompanyId" = (SELECT old_id FROM map_company);
UPDATE stg_document s SET "Id" = m.new_id FROM map_document m WHERE s."Id" = m.old_id;
UPDATE stg_document SET "CompanyId" = (SELECT new_id FROM map_company);
UPDATE stg_document s SET "DocumentTemplateId" = m.new_id FROM map_documenttemplate  m WHERE s."DocumentTemplateId" = m.old_id;
UPDATE stg_document s SET "EmployeeId"         = m.new_id FROM map_employee          m WHERE s."EmployeeId" = m.old_id;
UPDATE stg_document s SET "RequiredDocumentId" = m.new_id FROM map_requiredocuments  m WHERE s."RequiredDocumentId" = m.old_id;  -- singular
UPDATE stg_document SET "Name" = 'T - ' || "Name";
INSERT INTO "Documents" SELECT * FROM stg_document;

-- =========================== DocumentsUnits (SO o ultimo, "recem-criado")
-- Para cada Document, pega apenas o DocumentUnit mais recente
-- (Date DESC, Id DESC) e o recria como um pending novo:
--   Id novo (uuid), DocumentId remapeado, Status=Pending, sem
--   Name/Extension/Content, sem tokens de assinatura, sem WorkloadEndDate.
-- Period (Year/Month/Day/Week/Type) e Date sao PRESERVADOS do ultimo unit
-- (representam o periodo corrente do documento, como NewDocumentUnit faz).
CREATE TEMP TABLE stg_documentsunits ON COMMIT DROP AS
SELECT DISTINCT ON (du."DocumentId") du.*
FROM "DocumentsUnits" du
WHERE du."DocumentId" IN (SELECT old_id FROM map_document)
ORDER BY du."DocumentId", du."Date" DESC, du."Id" DESC;

UPDATE stg_documentsunits s SET "DocumentId" = m.new_id
FROM map_document m WHERE s."DocumentId" = m.old_id;

UPDATE stg_documentsunits SET
  "Id"                     = gen_random_uuid(),
  "Status"                 = 1,     -- DocumentUnitStatus.Pending
  "Content"                = '',
  "Name"                   = NULL,
  "Extension"              = NULL,
  "Validity"               = NULL,
  "SignatureDocumentToken" = NULL,
  "SignatureUrl"           = NULL,
  "AttachmentToken"        = NULL,
  "SentToSignatureAt"      = NULL,
  "WorkloadEndDate"        = NULL;

INSERT INTO "DocumentsUnits" SELECT * FROM stg_documentsunits;

-- ---------------------------------------------------------------------
-- Resumo do que foi clonado
-- ---------------------------------------------------------------------
DO $$
BEGIN
  RAISE NOTICE 'Nova empresa:      %', (SELECT new_id FROM map_company);
  RAISE NOTICE 'Employees:         %', (SELECT count(*) FROM map_employee);
  RAISE NOTICE 'Departments:       %', (SELECT count(*) FROM map_department);
  RAISE NOTICE 'Positions:         %', (SELECT count(*) FROM map_position);
  RAISE NOTICE 'Roles:             %', (SELECT count(*) FROM map_role);
  RAISE NOTICE 'Workplaces:        %', (SELECT count(*) FROM map_workplace);
  RAISE NOTICE 'DocumentTemplates: %', (SELECT count(*) FROM map_documenttemplate);
  RAISE NOTICE 'RequireDocuments:  %', (SELECT count(*) FROM map_requiredocuments);
  RAISE NOTICE 'Documents:         %', (SELECT count(*) FROM map_document);
  RAISE NOTICE 'DocumentsUnits:    % (1 pending por documento com historico)', (SELECT count(*) FROM stg_documentsunits);
END $$;

-- Troque por ROLLBACK para testar sem gravar.
COMMIT;
