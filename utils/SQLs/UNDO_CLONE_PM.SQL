-- =====================================================================
-- UNDO_CLONE_PM.SQL
-- Desfaz (apaga) COMPLETAMENTE uma empresa clonada e todos os seus dados.
-- Use para reverter uma clonagem feita por CLONE_COMPANY_PM.SQL.
--
-- USO:
--   psql "<conn_string>" -v ON_ERROR_STOP=1 \
--        -v company="<GUID_DA_EMPRESA_A_APAGAR>" \
--        -f UNDO_CLONE_PM.SQL
--
-- !!! PERIGO: apaga dados de verdade. CONFIRA o GUID: tem que ser o da
--     empresa CLONE, NUNCA o da origem. Rode um dry-run trocando o
--     COMMIT final por ROLLBACK antes de valer.
--
-- Deleta na ordem INVERSA das dependencias (FKs sao Restrict).
-- =====================================================================

BEGIN;

SET search_path TO people_management;

-- Trava de seguranca: aborta se o GUID nao existir como empresa.
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM "Companies" WHERE "Id" = :'company') THEN
    RAISE EXCEPTION 'Empresa % nao encontrada (parametro company)', :'company';
  END IF;
END $$;

-- 1) DocumentsUnits (filha de Documents)
DELETE FROM "DocumentsUnits" du USING "Documents" d
 WHERE du."DocumentId" = d."Id" AND d."CompanyId" = :'company';

-- 2) Documents
DELETE FROM "Documents" WHERE "CompanyId" = :'company';

-- 3) Join DocumentTemplateRequireDocuments (via RequireDocuments)
DELETE FROM "DocumentTemplateRequireDocuments" j USING "RequireDocuments" r
 WHERE j."RequireDocumentsId" = r."Id" AND r."CompanyId" = :'company';

-- 4) ListenEvent (filha de RequireDocuments)
DELETE FROM "ListenEvent" l USING "RequireDocuments" r
 WHERE l."RequireDocumentsId" = r."Id" AND r."CompanyId" = :'company';

-- 5) RequireDocuments
DELETE FROM "RequireDocuments" WHERE "CompanyId" = :'company';

-- 6) Dependent + EmploymentContract (filhas de Employees)
DELETE FROM "Dependent" d USING "Employees" e
 WHERE d."EmployeeId" = e."Id" AND e."CompanyId" = :'company';
DELETE FROM "EmploymentContract" ec USING "Employees" e
 WHERE ec."EmployeeId" = e."Id" AND e."CompanyId" = :'company';

-- 7) Employees
DELETE FROM "Employees" WHERE "CompanyId" = :'company';

-- 8) PlaceSignature (filha de DocumentTemplates)
DELETE FROM "PlaceSignature" p USING "DocumentTemplates" t
 WHERE p."DocumentTemplateId" = t."Id" AND t."CompanyId" = :'company';

-- 9) DocumentTemplates
DELETE FROM "DocumentTemplates" WHERE "CompanyId" = :'company';

-- 10) DocumentGroups
DELETE FROM "DocumentGroups" WHERE "CompanyId" = :'company';

-- 11) Roles
DELETE FROM "Roles" WHERE "CompanyId" = :'company';

-- 12) Positions
DELETE FROM "Positions" WHERE "CompanyId" = :'company';

-- 13) Workplaces
DELETE FROM "Workplaces" WHERE "CompanyId" = :'company';

-- 14) Departments
DELETE FROM "Departments" WHERE "CompanyId" = :'company';

-- 15) Companies
DELETE FROM "Companies" WHERE "Id" = :'company';

-- Troque por ROLLBACK para testar sem apagar.
COMMIT;
