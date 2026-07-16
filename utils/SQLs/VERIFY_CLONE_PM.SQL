-- =====================================================================
-- VERIFY_CLONE_PM.SQL
-- Verifica a integridade de uma clonagem feita por CLONE_COMPANY_PM.SQL.
-- Compara empresa ORIGEM x CLONE e checa FKs, arrays, DocumentsUnits e prefixo.
--
-- USO:
--   psql "<conn_string>" \
--        -v src="<GUID_ORIGEM>" \
--        -v cln="<GUID_CLONE>" \
--        -f VERIFY_CLONE_PM.SQL
--
-- Somente leitura (SELECT). Nao altera nada.
-- Resultado esperado: contagens iguais (exceto DocumentsUnits, por design),
-- todos os checks de "leak"/residuo = 0, prefixo total = com_prefixo.
-- =====================================================================
SET search_path TO people_management;

\echo '==================== 1) CONTAGENS: origem x clone ===================='
SELECT tabela, src_count, clone_count,
       CASE WHEN obs IS NOT NULL THEN obs
            WHEN src_count = clone_count THEN 'OK'
            ELSE '*** DIVERGENCIA ***' END AS status
FROM (
  SELECT 'Departments' tabela, 1 ord,
    (SELECT count(*) FROM "Departments" WHERE "CompanyId"=:'src') src_count,
    (SELECT count(*) FROM "Departments" WHERE "CompanyId"=:'cln') clone_count, NULL::text obs
  UNION ALL SELECT 'Workplaces',2,
    (SELECT count(*) FROM "Workplaces" WHERE "CompanyId"=:'src'),
    (SELECT count(*) FROM "Workplaces" WHERE "CompanyId"=:'cln'), NULL
  UNION ALL SELECT 'Positions',3,
    (SELECT count(*) FROM "Positions" WHERE "CompanyId"=:'src'),
    (SELECT count(*) FROM "Positions" WHERE "CompanyId"=:'cln'), NULL
  UNION ALL SELECT 'Roles',4,
    (SELECT count(*) FROM "Roles" WHERE "CompanyId"=:'src'),
    (SELECT count(*) FROM "Roles" WHERE "CompanyId"=:'cln'), NULL
  UNION ALL SELECT 'DocumentGroups',5,
    (SELECT count(*) FROM "DocumentGroups" WHERE "CompanyId"=:'src'),
    (SELECT count(*) FROM "DocumentGroups" WHERE "CompanyId"=:'cln'), NULL
  UNION ALL SELECT 'DocumentTemplates',6,
    (SELECT count(*) FROM "DocumentTemplates" WHERE "CompanyId"=:'src'),
    (SELECT count(*) FROM "DocumentTemplates" WHERE "CompanyId"=:'cln'), NULL
  UNION ALL SELECT 'PlaceSignature (filha)',7,
    (SELECT count(*) FROM "PlaceSignature" p JOIN "DocumentTemplates" t ON p."DocumentTemplateId"=t."Id" WHERE t."CompanyId"=:'src'),
    (SELECT count(*) FROM "PlaceSignature" p JOIN "DocumentTemplates" t ON p."DocumentTemplateId"=t."Id" WHERE t."CompanyId"=:'cln'), NULL
  UNION ALL SELECT 'Employees',8,
    (SELECT count(*) FROM "Employees" WHERE "CompanyId"=:'src'),
    (SELECT count(*) FROM "Employees" WHERE "CompanyId"=:'cln'), NULL
  UNION ALL SELECT 'Dependent (filha)',9,
    (SELECT count(*) FROM "Dependent" d JOIN "Employees" e ON d."EmployeeId"=e."Id" WHERE e."CompanyId"=:'src'),
    (SELECT count(*) FROM "Dependent" d JOIN "Employees" e ON d."EmployeeId"=e."Id" WHERE e."CompanyId"=:'cln'), NULL
  UNION ALL SELECT 'EmploymentContract (filha)',10,
    (SELECT count(*) FROM "EmploymentContract" ec JOIN "Employees" e ON ec."EmployeeId"=e."Id" WHERE e."CompanyId"=:'src'),
    (SELECT count(*) FROM "EmploymentContract" ec JOIN "Employees" e ON ec."EmployeeId"=e."Id" WHERE e."CompanyId"=:'cln'), NULL
  UNION ALL SELECT 'RequireDocuments',11,
    (SELECT count(*) FROM "RequireDocuments" WHERE "CompanyId"=:'src'),
    (SELECT count(*) FROM "RequireDocuments" WHERE "CompanyId"=:'cln'), NULL
  UNION ALL SELECT 'ListenEvent (filha)',12,
    (SELECT count(*) FROM "ListenEvent" l JOIN "RequireDocuments" r ON l."RequireDocumentsId"=r."Id" WHERE r."CompanyId"=:'src'),
    (SELECT count(*) FROM "ListenEvent" l JOIN "RequireDocuments" r ON l."RequireDocumentsId"=r."Id" WHERE r."CompanyId"=:'cln'), NULL
  UNION ALL SELECT 'DocTemplateRequireDocs (join)',13,
    (SELECT count(*) FROM "DocumentTemplateRequireDocuments" j JOIN "RequireDocuments" r ON j."RequireDocumentsId"=r."Id" WHERE r."CompanyId"=:'src'),
    (SELECT count(*) FROM "DocumentTemplateRequireDocuments" j JOIN "RequireDocuments" r ON j."RequireDocumentsId"=r."Id" WHERE r."CompanyId"=:'cln'), NULL
  UNION ALL SELECT 'Documents',14,
    (SELECT count(*) FROM "Documents" WHERE "CompanyId"=:'src'),
    (SELECT count(*) FROM "Documents" WHERE "CompanyId"=:'cln'), NULL
  UNION ALL SELECT 'DocumentsUnits',15,
    (SELECT count(*) FROM "DocumentsUnits" du JOIN "Documents" d ON du."DocumentId"=d."Id" WHERE d."CompanyId"=:'src'),
    (SELECT count(*) FROM "DocumentsUnits" du JOIN "Documents" d ON du."DocumentId"=d."Id" WHERE d."CompanyId"=:'cln'),
    'esperado: clone = 1 por documento (historico nao clonado)'
) q ORDER BY ord;

\echo '==================== 2) INTEGRIDADE FK (leaks p/ origem devem ser 0) ===================='
SELECT 'Positions.DepartmentId fora do clone' chk,
  (SELECT count(*) FROM "Positions" p WHERE p."CompanyId"=:'cln' AND p."DepartmentId" NOT IN (SELECT "Id" FROM "Departments" WHERE "CompanyId"=:'cln')) n
UNION ALL SELECT 'Roles.PositionId fora do clone',
  (SELECT count(*) FROM "Roles" r WHERE r."CompanyId"=:'cln' AND r."PositionId" NOT IN (SELECT "Id" FROM "Positions" WHERE "CompanyId"=:'cln'))
UNION ALL SELECT 'Employees.RoleId fora do clone',
  (SELECT count(*) FROM "Employees" e WHERE e."CompanyId"=:'cln' AND e."RoleId" NOT IN (SELECT "Id" FROM "Roles" WHERE "CompanyId"=:'cln'))
UNION ALL SELECT 'Employees.WorkPlaceId fora do clone',
  (SELECT count(*) FROM "Employees" e WHERE e."CompanyId"=:'cln' AND e."WorkPlaceId" NOT IN (SELECT "Id" FROM "Workplaces" WHERE "CompanyId"=:'cln'))
UNION ALL SELECT 'DocumentTemplates.GroupId fora do clone',
  (SELECT count(*) FROM "DocumentTemplates" t WHERE t."CompanyId"=:'cln' AND t."DocumentGroupId" NOT IN (SELECT "Id" FROM "DocumentGroups" WHERE "CompanyId"=:'cln'))
UNION ALL SELECT 'Documents.TemplateId fora do clone',
  (SELECT count(*) FROM "Documents" d WHERE d."CompanyId"=:'cln' AND d."DocumentTemplateId" NOT IN (SELECT "Id" FROM "DocumentTemplates" WHERE "CompanyId"=:'cln'))
UNION ALL SELECT 'Documents.EmployeeId fora do clone',
  (SELECT count(*) FROM "Documents" d WHERE d."CompanyId"=:'cln' AND d."EmployeeId" NOT IN (SELECT "Id" FROM "Employees" WHERE "CompanyId"=:'cln'))
UNION ALL SELECT 'Documents.RequiredDocumentId fora do clone',
  (SELECT count(*) FROM "Documents" d WHERE d."CompanyId"=:'cln' AND d."RequiredDocumentId" NOT IN (SELECT "Id" FROM "RequireDocuments" WHERE "CompanyId"=:'cln'));

\echo '==================== 3) RequireDocuments arrays: elementos ainda apontando p/ ORIGEM (deve ser 0) ===================='
SELECT 'AssociationIds -> origem' chk,
  (SELECT count(*) FROM "RequireDocuments" r, unnest(r."AssociationIds") elem
   WHERE r."CompanyId"=:'cln'
     AND elem IN (
       SELECT "Id" FROM "Employees"   WHERE "CompanyId"=:'src'
       UNION ALL SELECT "Id" FROM "Roles"       WHERE "CompanyId"=:'src'
       UNION ALL SELECT "Id" FROM "Positions"   WHERE "CompanyId"=:'src'
       UNION ALL SELECT "Id" FROM "Departments" WHERE "CompanyId"=:'src')) n
UNION ALL SELECT 'DocumentsTemplatesIds -> origem',
  (SELECT count(*) FROM "RequireDocuments" r, unnest(r."DocumentsTemplatesIds") elem
   WHERE r."CompanyId"=:'cln'
     AND elem IN (SELECT "Id" FROM "DocumentTemplates" WHERE "CompanyId"=:'src'));

\echo '==================== 4) DocumentsUnits: estado (Pending/limpos) e 1 por documento ===================='
SELECT
  (SELECT count(*) FROM "DocumentsUnits" du JOIN "Documents" d ON du."DocumentId"=d."Id" WHERE d."CompanyId"=:'cln') total_units,
  (SELECT count(DISTINCT du."DocumentId") FROM "DocumentsUnits" du JOIN "Documents" d ON du."DocumentId"=d."Id" WHERE d."CompanyId"=:'cln') docs_com_unit,
  (SELECT count(*) FROM "DocumentsUnits" du JOIN "Documents" d ON du."DocumentId"=d."Id" WHERE d."CompanyId"=:'cln' AND du."Status"<>1) nao_pending,
  (SELECT count(*) FROM "DocumentsUnits" du JOIN "Documents" d ON du."DocumentId"=d."Id" WHERE d."CompanyId"=:'cln' AND (du."Name" IS NOT NULL OR du."Content"<>'' OR du."SignatureUrl" IS NOT NULL OR du."SentToSignatureAt" IS NOT NULL OR du."AttachmentToken" IS NOT NULL)) com_residuo;

\echo '==================== 5) Prefixo "T - " nas colunas Name do clone (total deve = com_prefixo) ===================='
SELECT 'Departments' tabela,
  (SELECT count(*) FROM "Departments" WHERE "CompanyId"=:'cln') total,
  (SELECT count(*) FROM "Departments" WHERE "CompanyId"=:'cln' AND "Name" LIKE 'T - %') com_prefixo
UNION ALL SELECT 'Workplaces',(SELECT count(*) FROM "Workplaces" WHERE "CompanyId"=:'cln'),(SELECT count(*) FROM "Workplaces" WHERE "CompanyId"=:'cln' AND "Name" LIKE 'T - %')
UNION ALL SELECT 'Positions',(SELECT count(*) FROM "Positions" WHERE "CompanyId"=:'cln'),(SELECT count(*) FROM "Positions" WHERE "CompanyId"=:'cln' AND "Name" LIKE 'T - %')
UNION ALL SELECT 'Roles',(SELECT count(*) FROM "Roles" WHERE "CompanyId"=:'cln'),(SELECT count(*) FROM "Roles" WHERE "CompanyId"=:'cln' AND "Name" LIKE 'T - %')
UNION ALL SELECT 'DocumentGroups',(SELECT count(*) FROM "DocumentGroups" WHERE "CompanyId"=:'cln'),(SELECT count(*) FROM "DocumentGroups" WHERE "CompanyId"=:'cln' AND "Name" LIKE 'T - %')
UNION ALL SELECT 'DocumentTemplates',(SELECT count(*) FROM "DocumentTemplates" WHERE "CompanyId"=:'cln'),(SELECT count(*) FROM "DocumentTemplates" WHERE "CompanyId"=:'cln' AND "Name" LIKE 'T - %')
UNION ALL SELECT 'Employees',(SELECT count(*) FROM "Employees" WHERE "CompanyId"=:'cln'),(SELECT count(*) FROM "Employees" WHERE "CompanyId"=:'cln' AND "Name" LIKE 'T - %')
UNION ALL SELECT 'RequireDocuments',(SELECT count(*) FROM "RequireDocuments" WHERE "CompanyId"=:'cln'),(SELECT count(*) FROM "RequireDocuments" WHERE "CompanyId"=:'cln' AND "Name" LIKE 'T - %')
UNION ALL SELECT 'Documents',(SELECT count(*) FROM "Documents" WHERE "CompanyId"=:'cln'),(SELECT count(*) FROM "Documents" WHERE "CompanyId"=:'cln' AND "Name" LIKE 'T - %')
UNION ALL SELECT 'Dependent',(SELECT count(*) FROM "Dependent" d JOIN "Employees" e ON d."EmployeeId"=e."Id" WHERE e."CompanyId"=:'cln'),(SELECT count(*) FROM "Dependent" d JOIN "Employees" e ON d."EmployeeId"=e."Id" WHERE e."CompanyId"=:'cln' AND d."Name" LIKE 'T - %');
