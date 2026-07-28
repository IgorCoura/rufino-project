-- Diagnóstico para a Fase 3(a): mover a competência do evento para o DocumentTemplate.
--
-- Hoje quem decide a competência é a frequência do evento que disparou. Como um template pode ser alcançado
-- por vários RequireDocuments — e cada um escuta os seus eventos —, o mesmo template pode implicar frequências
-- diferentes. A validação ListenEventsIsValid só impede divergência DENTRO de um RequireDocuments; nada impede
-- entre RequireDocuments distintos.
--
-- Quando a competência virar configuração do template, ele só poderá ter UMA. Esta query separa:
--   OK                 -> uma frequência só: a migration pode derivar sem ambiguidade
--   CONFLITO           -> mais de uma: precisa de escolha humana
--   SEM COMPETENCIA    -> nenhum evento recorrente com frequência: nada a migrar
--
-- Mapeamento de frequência copiado de RecurringEvents.GetFrequency().

WITH evento_frequencia AS (
    SELECT
        dtr."DocumentTemplateId" AS template_id,
        CASE
            WHEN le."EventId" = 1013 THEN 'Daily'
            WHEN le."EventId" = 1014 THEN 'Weekly'
            WHEN le."EventId" = 1015 THEN 'Monthly'
            WHEN le."EventId" = 1016 THEN 'Yearly'
            WHEN le."EventId" BETWEEN 1001 AND 1012 THEN 'Monthly'  -- meses do ano
            ELSE NULL                                                -- MINUTELY e não recorrentes
        END AS frequencia
    FROM people_management."DocumentTemplateRequireDocuments" dtr
    JOIN people_management."ListenEvent" le
      ON le."RequireDocumentsId" = dtr."RequireDocumentsId"
),
por_template AS (
    SELECT
        template_id,
        array_agg(DISTINCT frequencia) FILTER (WHERE frequencia IS NOT NULL) AS frequencias,
        count(DISTINCT frequencia) FILTER (WHERE frequencia IS NOT NULL) AS qtd
    FROM evento_frequencia
    GROUP BY template_id
)
SELECT
    dt."Name"        AS template,
    c."Name"         AS empresa,
    COALESCE(pt.frequencias, ARRAY[]::text[]) AS frequencias_implicadas,
    CASE
        WHEN COALESCE(pt.qtd, 0) = 0 THEN 'SEM COMPETENCIA'
        WHEN pt.qtd = 1              THEN 'OK'
        ELSE                              'CONFLITO'
    END AS situacao
FROM people_management."DocumentTemplates" dt
JOIN people_management."Companies" c ON c."Id" = dt."CompanyId"
LEFT JOIN por_template pt ON pt.template_id = dt."Id"
ORDER BY situacao DESC, c."Name", dt."Name";

-- Resumo: o número que decide o plano.
--
-- WITH ... (repita os CTEs acima) ...
-- SELECT situacao, count(*) FROM (...) GROUP BY situacao;
