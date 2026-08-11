namespace BillPayment.Infra;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Payees;
using BillPayment.Domain.Ports;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.TrustedOrigins;
using Amazon.S3;
using BillPayment.Infra.Asaas;
using BillPayment.Infra.BankDirectory;
using BillPayment.Infra.DocumentIntelligence;
using BillPayment.Infra.DocumentIntelligence.Gemini;
using BillPayment.Infra.Extraction;
using BillPayment.Infra.Idempotency;
using BillPayment.Infra.Mailboxes;
using BillPayment.Infra.Mailboxes.Graph;
using BillPayment.Infra.Outbox;
using BillPayment.Infra.Persistence;
using BillPayment.Infra.Repositories;
using BillPayment.Infra.Secrets;
using BillPayment.Infra.Storage;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class InfraDependencies
{
    public static IServiceCollection AddInfraDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BillPaymentDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("BillPayment"), npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", BillPaymentDbContext.DEFAULT_SCHEMA);
                npgsql.EnableRetryOnFailure(3);
            });
            options.UseExceptionProcessor();
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BillPaymentDbContext>());
        services.AddScoped<IRequestManager, RequestManager>();

        // Um repositório por Aggregate Root. Entidades internas são acessadas pela raiz.
        services.AddScoped<ITrustedOriginRepository, TrustedOriginRepository>();
        services.AddScoped<IBillRepository, BillRepository>();
        services.AddScoped<IPayeeRepository, PayeeRepository>();
        services.AddScoped<IPayerProfileRepository, PayerProfileRepository>();
        services.AddScoped<ICaptureSourceRepository, CaptureSourceRepository>();
        services.AddScoped<ICaptureItemRepository, CaptureItemRepository>();

        services.AddMailboxReader(configuration);

        // Cascata de extração. Sem opção de desligar: é determinística, local e gratuita — e é
        // ela que permite descartar o que não é boleto sem encher fila.
        services.Configure<ExtractionOptions>(configuration.GetSection(ExtractionOptions.SectionName));
        services.AddScoped<IBoletoDocumentParser, PdfBoletoDocumentParser>();

        services.AddAttachmentStorage(configuration);
        services.AddDocumentIntelligence(configuration);

        // Singleton: o snapshot do Bacen é lido do assembly uma vez e é imutável depois.
        services.AddSingleton<IBankDirectory, BacenBankDirectory>();

        // Relógio injetável: adapters e cofre carimbam instante de consulta e de gravação, e
        // testar isso com DateTimeOffset.UtcNow inline é impossível.
        services.TryAddSingleton(TimeProvider.System);

        services.AddOutbox(configuration);
        services.AddAsaasLookup(configuration);
        services.AddSecretVault(configuration);

        return services;
    }

    /// <summary>
    /// Leitura de caixa de e-mail. Sem <c>Graph:Enabled</c>, entra o substituto que devolve
    /// <c>Unavailable</c> — e conectar uma fonte passa a falhar na prova de acesso, em vez de
    /// criar uma caixa que nunca sincronizaria.
    /// </summary>
    /// <remarks>
    /// <strong>Não há credencial de instalação aqui.</strong> A credencial é por fonte, cifrada
    /// no cofre — cada cliente registra o próprio aplicativo no Entra ID dele (ADR-006). Por isso
    /// o gatilho é um booleano, e não a presença de um segredo como no Asaas.
    /// <para>
    /// Os dois clientes retentam, e podem: listar mensagens e pedir token são idempotentes. A
    /// mesma ressalva do Asaas vale — um adapter que <em>escreva</em> jamais reaproveita cliente
    /// com retry.
    /// </para>
    /// </remarks>
    private static void AddMailboxReader(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(GraphOptions.SectionName);
        services.Configure<GraphOptions>(section);

        if (!section.GetValue<bool>(nameof(GraphOptions.Enabled)))
        {
            services.TryAddScoped<IMailboxReader, UnconfiguredMailboxReader>();
            return;
        }

        var options = section.Get<GraphOptions>() ?? new GraphOptions();
        var timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

        services.AddHttpClient(GraphHttp.CLIENT_NAME, http => http.Timeout = timeout)
            .AddStandardResilienceHandler();

        services.AddHttpClient(GraphHttp.TOKEN_CLIENT_NAME, http => http.Timeout = timeout)
            .AddStandardResilienceHandler();

        // Singleton para o cache de token sobreviver entre varreduras — pedir um token por
        // varredura faria o Entra ID limitar a taxa da própria autenticação.
        services.AddSingleton<GraphTokenProvider>();
        services.AddScoped<IMailboxReader, GraphMailboxReader>();
    }

    /// <summary>
    /// Armazenamento dos artefatos capturados, em serviço compatível com S3 auto-hospedado.
    /// </summary>
    /// <remarks>
    /// Sem configuração entra o substituto que <strong>falha em toda escrita e leitura</strong> —
    /// guardar em lugar nenhum sem avisar faria o sistema pagar boleto cujo original ninguém
    /// consegue recuperar depois.
    /// </remarks>
    private static void AddAttachmentStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(StorageOptions.SectionName);
        services.Configure<StorageOptions>(section);

        var options = section.Get<StorageOptions>() ?? new StorageOptions();

        if (!options.IsConfigured)
        {
            services.AddSingleton<IAttachmentStorage, UnconfiguredAttachmentStorage>();
            return;
        }

        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
            new Amazon.Runtime.BasicAWSCredentials(options.AccessKey, options.SecretKey),
            new AmazonS3Config
            {
                ServiceURL = options.ServiceUrl,
                ForcePathStyle = options.ForcePathStyle,

                // Sem região explícita o SDK infere uma da AWS e assina com ela; o serviço
                // auto-hospedado recusa a assinatura e a falha só aparece ao gravar o primeiro
                // anexo. IsConfigured garante que o valor existe.
                AuthenticationRegion = options.AuthenticationRegion,
            }));

        services.AddScoped<IAttachmentStorage, S3AttachmentStorage>();
    }

    /// <summary>
    /// Extrator de documentos por IA — o degrau 3 da cascata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sem provedor ou sem chave entra o <c>NullDocumentIntelligence</c>, que devolve vazio: a
    /// cascata termina no parser determinístico e o que não resolve vai para a quarentena, como
    /// antes da 2.4. É degradação, não falha — ao contrário do armazenamento, cuja ausência
    /// perderia um comprovante que ninguém recupera.
    /// </para>
    /// <para>
    /// <strong>Este cliente NÃO retenta</strong>, e é a diferença em relação ao Asaas e ao Graph:
    /// cada tentativa consome cota de uma conta com teto diário, e insistir num PDF que o modelo
    /// recusou gastaria o dia num documento só. A retentativa é a fila de quarentena, no dia
    /// seguinte — mais barata e visível.
    /// </para>
    /// </remarks>
    private static void AddDocumentIntelligence(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(DocumentIntelligenceOptions.SectionName);
        services.Configure<DocumentIntelligenceOptions>(section);

        var options = section.Get<DocumentIntelligenceOptions>() ?? new DocumentIntelligenceOptions();

        if (!options.IsConfigured)
        {
            services.AddSingleton<IDocumentIntelligence, NullDocumentIntelligence>();
            return;
        }

        // Singleton: o teto diário e o intervalo mínimo só significam alguma coisa se o contador
        // for o mesmo entre requisições.
        services.AddSingleton<ExtractionBudget>();

        services.AddHttpClient(GeminiDocumentIntelligence.CLIENT_NAME, http =>
        {
            http.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

            // A chave vai no cabeçalho, não na query string: URL entra em log de proxy e em
            // telemetria de cliente HTTP, e segredo em log é segredo vazado.
            http.DefaultRequestHeaders.Add("x-goog-api-key", options.ApiKey);
        });

        services.AddScoped<IDocumentIntelligence, GeminiDocumentIntelligence>();
    }

    /// <summary>
    /// Consulta oficial nos dois trilhos. Sem chave configurada, entram os substitutos que
    /// devolvem <c>Unavailable</c> — ver <c>UnconfiguredLookupServices</c>.
    /// </summary>
    /// <remarks>
    /// <strong>Este cliente retenta, e por isso não serve para pagar.</strong> Simular e
    /// decodificar são idempotentes; o adapter de pagamento da fase 3 precisa de um cliente
    /// próprio sem retry automático — o endpoint de pagamento Pix não documenta idempotência e
    /// uma retentativa de rede pagaria duas vezes (<c>04-integrations.md</c>).
    /// </remarks>
    private static void AddAsaasLookup(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(AsaasOptions.SectionName);
        services.Configure<AsaasOptions>(section);

        var options = section.Get<AsaasOptions>() ?? new AsaasOptions();

        if (!options.IsConfigured)
        {
            services.AddSingleton<IBillLookupService, UnconfiguredBillLookupService>();
            services.AddSingleton<IPixLookupService, UnconfiguredPixLookupService>();
            return;
        }

        services.AddHttpClient<IBillLookupService, AsaasBillLookupService>(ConfigureAsaasClient(options))
            .AddStandardResilienceHandler();

        services.AddHttpClient<IPixLookupService, AsaasPixLookupService>(ConfigureAsaasClient(options))
            .AddStandardResilienceHandler();
    }

    private static Action<HttpClient> ConfigureAsaasClient(AsaasOptions options)
        => client =>
        {
            // A barra final importa: sem ela, BaseAddress descarta o segmento "/v3" ao
            // combinar com um caminho relativo.
            var baseUrl = options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/";

            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("access_token", options.ApiKey);
        };

    private static void AddSecretVault(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SecretsOptions.SectionName);
        services.Configure<SecretsOptions>(section);

        var options = section.Get<SecretsOptions>() ?? new SecretsOptions();

        if (options.ResolveMasterKey() is null)
        {
            services.AddScoped<ISecretVault, UnconfiguredSecretVault>();
            return;
        }

        services.AddScoped<ISecretVault>(sp => new EnvelopeSecretVault(
            sp.GetRequiredService<BillPaymentDbContext>(),
            sp.GetRequiredService<TimeProvider>(),
            options));
    }

    private static void AddOutbox(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));

        services.AddDomainEventDispatcher();
        services.AddSingleton<IOutboxEventTypeResolver, OutboxEventTypeResolver>();
        services.AddSingleton<IOutboxProcessor, OutboxProcessor>();

        // Handlers de Domain Event (IDomainEventHandler<TEvent>) entram aqui junto com os primeiros eventos do BC.

        var enabled = configuration.GetSection(OutboxOptions.SectionName).GetValue<bool?>("Enabled") ?? true;
        if (enabled)
            services.AddHostedService<OutboxBackgroundService>();
    }

    // Public so the in-process dispatcher can be wired in isolation (e.g. tests) without the full outbox stack.
    public static IServiceCollection AddDomainEventDispatcher(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        return services;
    }
}
