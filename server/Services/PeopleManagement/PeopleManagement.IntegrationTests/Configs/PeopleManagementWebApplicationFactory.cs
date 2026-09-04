using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Testcontainers.LocalStack;
using EntityFramework.Exceptions.PostgreSQL;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PeopleManagement.API.Authorization;
using PeopleManagement.Infra.Context;
using Respawn;
using Testcontainers.PostgreSql;

namespace PeopleManagement.IntegrationTests.Configs
{

    public class PeopleManagementWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15.1-alpine")
            .WithDatabase("PeopleManagementTestDb")
            .WithUsername("admin")
            .WithPassword("admin")
            .Build();

        private readonly LocalStackContainer _localStackContainer = new LocalStackBuilder()
           .Build();

        private Respawner _respawner = default!;
        private string _connectionString = default!;

        // Scopes criados por GetContext() são rastreados e descartados a cada reset (por teste), evitando
        // que cada chamada deixe um IServiceScope pendurado até a coleta de lixo.
        private readonly List<IServiceScope> _trackedScopes = [];

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();
            await _localStackContainer.StartAsync();
            _connectionString = _dbContainer.GetConnectionString();

            // Força a construção do host: roda as migrations (cria o schema people_management) e registra os jobs.
            // Sob o ambiente "IntegrationTest" o PopulateDb não roda, então o baseline já nasce limpo.
            _ = Services;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["people_management"],

                // O histórico de migrações vive dentro de people_management e NÃO é dado de teste:
                // apagá-lo faz o próximo host subir achando que o banco está vazio e tentar criar tudo
                // outra vez, morrendo em 42P07 "relation already exists".
                TablesToIgnore = [new Respawn.Graph.Table("people_management", "__EFMigrationsHistory")],
            });
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Ambiente dedicado de teste: desliga os workers do Hangfire (gate no Program.cs) e o seed do PopulateDb.
            builder.UseEnvironment("IntegrationTest");

            // O limitador de taxa fica DESLIGADO na suíte: os testes batem no mesmo usuário centenas
            // de vezes por minuto e estourariam o teto global, produzindo 429 em teste que nada tem a
            // ver com limite. A prova de que o limitador funciona vive em RateLimitingTests, num host
            // próprio com teto de 2.
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:Enabled"] = "false",

                    // O cache do retrato de permissoes fica DESLIGADO na suite: os testes
                    // batem no mesmo host trocando o cenario de permissao, e o cache — que e
                    // chaveado pelo token, como tem que ser — devolveria o retrato anterior.
                    // Quem prova que ele funciona e RptCacheTests, que troca o token de proposito.
                    ["Keycloak:RptCacheEnabled"] = "false",
                });
            });

            builder.ConfigureTestServices(services =>
            {
                //Config DataBase
                var dbContext = services.SingleOrDefault(
                        d => d.ServiceType ==
                            typeof(IDbContextFactory<PeopleManagementContext>));
                if (dbContext != null)
                    services.Remove(dbContext);

                services.AddDbContextFactory<PeopleManagementContext>(options =>
                {
                    options.UseNpgsql(
                        _dbContainer.GetConnectionString(),
                        npgsqlOptionsAction: sqlOptions =>
                        {
                            // Repete o que o Program.cs configura. Esta registração é a última, então é
                            // ela que vale para o contexto resolvido pelo DI — inclusive o que roda o
                            // Migrate. Sem isto a suíte validaria um layout de histórico que o deploy
                            // nunca produz, que é justamente o tipo de defeito que ela existe para pegar.
                            sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", PeopleManagementContext.DEFAULT_SCHEMA);
                            sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
                        }).UseExceptionProcessor()
                        .EnableDetailedErrors()
                        .EnableSensitiveDataLogging();
                }, ServiceLifetime.Scoped); // Alinha com o Program.cs: factory Scoped evita o conflito "singleton IDbContextFactory consumindo scoped DbContextOptions".


                //Config Hangfire — mantém apenas o storage/client apontando para o container de teste.
                // Os workers (servers) NÃO são registrados neste ambiente (gate no Program.cs), então nenhum job
                // é processado: agendamento e JobStorage seguem funcionando para inspeção, sem efeitos colaterais.
                services.AddHangfire(configuration =>
                configuration.UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(_dbContainer.GetConnectionString());
                }));

                //Config S3
                var amazonS3 = services.SingleOrDefault(d => d.ServiceType == typeof(IAmazonS3));
                if (amazonS3 != null)
                    services.Remove(amazonS3);

                services.AddSingleton<IAmazonS3>(_ =>
                {
                    var config = new AmazonS3Config
                    {
                        ServiceURL = _localStackContainer.GetConnectionString(),
                        ForcePathStyle = true
                    };
                    return new AmazonS3Client(new BasicAWSCredentials("test", "test"), config);
                });

                //Auth

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = MockAuthenticationHandler.AuthScheme;
                    options.DefaultChallengeScheme = MockAuthenticationHandler.AuthScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, MockAuthenticationHandler>(MockAuthenticationHandler.AuthScheme, options => { });

                services.AddSingleton<IAuthorizationHandler, MockAccessRequirementHandler>();

                // O que a suíte substitui é a ida ao Keycloak (o ProtectedResourceRequirement), NUNCA
                // o nome do parâmetro de rota nem o do claim: os dois vêm do mesmo AuthorizationOptions
                // que a produção monta. Escritos à mão aqui, trocar o claim no appsettings deixava a
                // suíte verde exercitando um guard que o deploy não produz — foi o que aconteceu quando
                // "companies" virou "pm_tenants" e nada quebrou.
                services.AddSingleton<IAuthorizationPolicyProvider>(x =>
                {
                    var authorizationOptions = x.GetRequiredService<API.Authorization.AuthorizationOptions>();

                    return new ProtectedResourcePolicyProvider(param =>
                    {
                        var policy = new AuthorizationPolicyBuilder(MockAuthenticationHandler.AuthScheme);
                        policy.AddRequirements(new MockAccessRequirement(
                            authorizationOptions.RouteNameRequirement,
                            authorizationOptions.RouteClaimTypeRequirement));
                        return policy;
                    });
                });
            });
        }

        public PeopleManagementContext GetContext()
        {
            var scope = this.Services.CreateScope();
            _trackedScopes.Add(scope);
            return scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
        }

        // Reseta o schema people_management entre os testes (chamado pelo DisposeAsync da BaseIntegrationTest).
        public async Task ResetDatabaseAsync()
        {
            foreach (var scope in _trackedScopes)
                scope.Dispose();
            _trackedScopes.Clear();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await _respawner.ResetAsync(connection);
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await _dbContainer.StopAsync();
            await _localStackContainer.StopAsync();
        }
    }

}
