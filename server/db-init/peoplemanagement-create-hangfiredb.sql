-- Executado pelo entrypoint do Postgres SÓ quando o volume está vazio (initdb).
-- O database principal (PeopleManagementDb) vem de POSTGRES_DB; este cria o segundo,
-- que o Hangfire usa — ele cria tabelas, nunca databases.
CREATE DATABASE hangfiredb;
