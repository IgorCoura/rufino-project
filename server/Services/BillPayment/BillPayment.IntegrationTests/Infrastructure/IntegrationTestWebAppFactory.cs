namespace BillPayment.IntegrationTests.Infrastructure;

using BillPayment.Domain.Ports;
using BillPayment.Infra.Persistence;
using BillPayment.Infra.Secrets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("bill_payment_tests")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    /// <summary>Master key só desta execução — nunca versionada, nunca reaproveitada.</summary>
    public static readonly string TestMasterKey = SecretsOptions.GenerateMasterKey();

    private Respawner _respawner = default!;
    private string _connectionString = default!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:BillPayment", _connectionString);
        // Disable the polling worker in tests so the outbox is driven deterministically via IOutboxProcessor.
        builder.UseSetting("Outbox:Enabled", "false");

        // Master key descartável, gerada por execução. O cofre precisa de uma para existir, e
        // a suíte não pode depender de segredo de máquina nem carregar um valor versionado.
        // Nenhuma chave do Asaas é configurada de propósito: os testes não devem ter
        // credencial capaz de pagar contas, então a consulta oficial cai nos substitutos.
        builder.UseSetting("Secrets:MasterKey", TestMasterKey);

        // A suíte roda como Development, então ela lê o appsettings.Development.json E os
        // user-secrets da máquina de quem está desenvolvendo. Nada disso pode dirigir os testes:
        //  - Capture ligado registraria os dois workers, que varreriam e processariam por conta
        //    própria enquanto o Respawn limpa o banco entre testes.
        //  - Graph ligado trocaria o UnconfiguredMailboxReader por um adapter que tenta rede.
        //  - Storage configurado apontaria a fábrica compartilhada para um balde de verdade,
        //    justamente onde ela existe para exercitar o armazenamento NÃO configurado.
        // Cada classe que precisa da cadeia pede um host irmão (WithCaptureChain etc.).
        builder.UseSetting("Capture:Enabled", "false");
        builder.UseSetting("Graph:Enabled", "false");
        builder.UseSetting("Storage:ServiceUrl", string.Empty);

        // A chave do Asaas idem, e esta já era regra escrita — só não estava executável. Quem a
        // sustentava era a ausência do segredo na máquina, e no dia em que alguém rodou
        // `dotnet user-secrets set "Asaas:ApiKey"` a suíte passou a fazer chamada de rede ao
        // provedor: os UnconfiguredLookupTests, que existem para provar que SEM chave a consulta
        // degrada para Unavailable, voltaram Unresolved — resposta real do Asaas. Além de não
        // provarem mais nada, tornam a suíte refém do provedor estar no ar.
        builder.UseSetting("Asaas:ApiKey", string.Empty);

        // O extrator de visão idem, e aqui o risco é maior que o do Asaas: a chave do Gemini está
        // no user-secrets e o perfil de dev liga o provedor, então sem isto a suíte gastaria cota
        // de uma conta gratuita a cada execução — e os testes deixariam de ser determinísticos,
        // porque a mesma entrada pode devolver leituras diferentes. Quem exercita o degrau 3 pede
        // WithCaptureChain(), que injeta o FakeDocumentIntelligence.
        builder.UseSetting("DocumentIntelligence:Provider", "None");

        // A escada de resolução de link idem, e esta é a única que faz requisição de saída para
        // um servidor de TERCEIRO — não para um provedor com quem há contrato. Ligada, a suíte
        // buscaria endereços de verdade em máquina de desenvolvedor e em CI, com resultado
        // dependente de o servidor do emissor estar no ar. Quem exercita os degraus 2 e 3 injeta
        // o resolvedor de teste.
        builder.UseSetting("LinkResolution:Enabled", "false");

        builder.UseEnvironment("Development");
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();

        // A tabela de histórico TEM que ser a mesma que a Infra configura. Este contexto é
        // construído à mão, fora do DI, então ele não herda o MigrationsHistoryTable do
        // AddInfraDependencies — e com o padrão do EF o histórico iria para outro lugar: a
        // fábrica migraria, o host não acharia registro nenhum, tentaria criar tudo de novo e
        // morreria em 42P07 "relation already exists", derrubando a suíte inteira.
        var options = new DbContextOptionsBuilder<BillPaymentDbContext>()
            .UseNpgsql(_connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", BillPaymentDbContext.DEFAULT_SCHEMA))
            .Options;

        // Migrações, como em produção. Com EnsureCreatedAsync a suíte validava um schema que o
        // deploy nunca produziria — e era justamente por isso que ela não pegava o defeito de
        // schema desatualizado: o Testcontainers sobe um Postgres vazio a cada execução, o único
        // caso em que EnsureCreated funciona.
        await using var context = new BillPaymentDbContext(options);
        await context.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["bill_payment"],

            // O histórico de migrações NÃO é dado de teste: apagá-lo faz o próximo host subir
            // achando que o banco está vazio e tentar criar tudo outra vez, morrendo em
            // 42P07 "relation already exists". Com EnsureCreatedAsync isto não aparecia porque
            // ele é no-op quando há qualquer tabela — foi o primeiro efeito colateral de trocar
            // para MigrateAsync, e derrubou 192 testes de uma vez.
            TablesToIgnore = [new Respawn.Graph.Table("bill_payment", "__ef_migrations_history")],
        });
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Host irmão com a consulta oficial trocada por uma determinística.
    /// </summary>
    /// <remarks>
    /// <strong>A troca é por classe de teste, não global.</strong> Trocá-la na fábrica
    /// compartilhada derrubaria os testes que provam justamente o contrário — que sem chave
    /// configurada a consulta degrada para <c>Unavailable</c> e nenhum boleto é dado como
    /// verificado. O contêiner e o banco continuam sendo os mesmos.
    /// </remarks>
    public WebApplicationFactory<Program> WithFakeLookups()
        => WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<FakeLookupServices>();
            services.RemoveAll<IBillLookupService>();
            services.RemoveAll<IPixLookupService>();
            services.AddSingleton<IBillLookupService>(sp => sp.GetRequiredService<FakeLookupServices>());
            services.AddSingleton<IPixLookupService>(sp => sp.GetRequiredService<FakeLookupServices>());
        }));

    /// <summary>
    /// Host irmão com a leitura de caixa trocada por uma que sempre concede acesso.
    /// </summary>
    /// <remarks>
    /// Mesma regra do <see cref="WithFakeLookups"/>: a troca é <strong>por classe de teste</strong>.
    /// A fábrica compartilhada continua com o <c>UnconfiguredMailboxReader</c> justamente porque
    /// há teste provando que, sem adapter, conectar uma fonte <em>falha</em> em vez de criar uma
    /// caixa silenciosa.
    /// </remarks>
    public WebApplicationFactory<Program> WithReachableMailbox()
        => WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<FakeMailboxReader>();
            services.RemoveAll<IMailboxReader>();
            services.AddSingleton<IMailboxReader>(sp => sp.GetRequiredService<FakeMailboxReader>());
        }));

    /// <summary>
    /// Host irmão com a cadeia de captura completa: caixa falsa e armazenamento em memória.
    /// </summary>
    /// <remarks>
    /// Mesma regra dos outros substitutos — a troca é <strong>por classe de teste</strong>. A
    /// fábrica compartilhada mantém o armazenamento não configurado, que falha em toda escrita,
    /// porque é assim que a aplicação se comporta antes de alguém configurar o balde.
    /// </remarks>
    public WebApplicationFactory<Program> WithCaptureChain()
        => WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<FakeMailboxReader>();
            services.RemoveAll<IMailboxReader>();
            services.AddSingleton<IMailboxReader>(sp => sp.GetRequiredService<FakeMailboxReader>());

            services.AddSingleton<InMemoryAttachmentStorage>();
            services.RemoveAll<IAttachmentStorage>();
            services.AddSingleton<IAttachmentStorage>(sp => sp.GetRequiredService<InMemoryAttachmentStorage>());

            // O extrator de visão entra junto, e desligado por padrão (`Result` vazio): assim os
            // testes da cascata determinística que já existiam continuam medindo o que mediam, e
            // quem quer exercitar o degrau 3 programa a resposta — inclusive a errada.
            services.AddSingleton<FakeDocumentIntelligence>();
            services.RemoveAll<IDocumentIntelligence>();
            services.AddSingleton<IDocumentIntelligence>(sp => sp.GetRequiredService<FakeDocumentIntelligence>());

            // A escada de link entra pelo mesmo caminho e também desligada por padrão. O
            // resolvedor de verdade sai de cena aqui: ele faz requisição para servidor de
            // terceiro, e um teste que dependesse disso passaria a medir se o emissor está no ar.
            services.AddSingleton<FakeDocumentLinkResolver>();
            services.RemoveAll<IDocumentLinkResolver>();
            services.AddSingleton<IDocumentLinkResolver>(sp => sp.GetRequiredService<FakeDocumentLinkResolver>());
        }));

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public async Task ExecuteDbContextAsync(Func<BillPaymentDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BillPaymentDbContext>();
        await action(context);
    }

    public async Task<T> ExecuteDbContextAsync<T>(Func<BillPaymentDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BillPaymentDbContext>();
        return await action(context);
    }
}
