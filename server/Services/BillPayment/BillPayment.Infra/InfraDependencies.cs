namespace BillPayment.Infra;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.Retention;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Payees;
using BillPayment.Infra.Notifications;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Notifications;
using BillPayment.Domain.Ports;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.TrustedOrigins;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Amazon.S3;
using BillPayment.Infra.Asaas;
using BillPayment.Infra.BankDirectory;
using BillPayment.Infra.DocumentIntelligence;
using BillPayment.Infra.DocumentIntelligence.Gemini;
using BillPayment.Infra.Extraction;
using BillPayment.Infra.Extraction.Links;
using BillPayment.Infra.Idempotency;
using BillPayment.Infra.Mailboxes;
using BillPayment.Infra.Mailboxes.Graph;
using BillPayment.Infra.Outbox;
using BillPayment.Infra.Persistence;
using BillPayment.Infra.Repositories;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Infra.Payments;
using BillPayment.Infra.Secrets;
using BillPayment.Infra.Storage;
using BillPayment.Infra.WorkingDays;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
        services.AddScoped<IPaymentWebhookLedger, PaymentWebhookLedger>();

        // Um repositório por Aggregate Root. Entidades internas são acessadas pela raiz.
        services.AddScoped<ITrustedOriginRepository, TrustedOriginRepository>();
        services.AddScoped<IBillRepository, BillRepository>();
        services.AddScoped<IPayeeRepository, PayeeRepository>();
        services.AddScoped<IPayerProfileRepository, PayerProfileRepository>();
        services.AddScoped<ICaptureSourceRepository, CaptureSourceRepository>();
        services.AddScoped<ICaptureItemRepository, CaptureItemRepository>();
        services.AddScoped<ICapturedMessageRepository, CapturedMessageRepository>();
        services.AddScoped<ICaptureRetentionPolicyRepository, CaptureRetentionPolicyRepository>();
        services.AddScoped<IBillExpectationRepository, BillExpectationRepository>();
        services.AddScoped<ITenantNotificationSettingsRepository, TenantNotificationSettingsRepository>();
        services.AddScoped<IPaymentOrderRepository, PaymentOrderRepository>();

        services.AddNotifications(configuration);

        services.AddMailboxReader(configuration);

        // Cascata de extração. Sem opção de desligar: é determinística, local e gratuita — e é
        // ela que permite descartar o que não é boleto sem encher fila.
        services.Configure<ExtractionOptions>(configuration.GetSection(ExtractionOptions.SectionName));
        services.AddScoped<PdfBoletoDocumentParser>();
        services.AddScoped<EmailBodyDocumentParser>();
        services.AddScoped<IBoletoDocumentParser, CascadingBoletoDocumentParser>();

        services.AddAttachmentStorage(configuration);
        services.AddDocumentIntelligence(configuration);
        services.AddLinkResolution(configuration);

        // Singleton: o snapshot do Bacen é lido do assembly uma vez e é imutável depois.
        services.AddSingleton<IBankDirectory, BacenBankDirectory>();

        // Singleton pela mesma doutrina: o calendário é calculado, imutável e sem I/O.
        services.AddSingleton<IWorkingDayCalendar, BrazilianWorkingDayCalendar>();

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
    /// Canal de aviso da expectativa (ADR-014).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>O log recebe o aviso SEMPRE, e o canal externo entra por cima.</strong> Não é
    /// substituto que falha alto, ao contrário do cofre e do armazenamento: derrubar a varredura
    /// porque o e-mail não está configurado apagaria o <em>registro</em> do alerta, e é ele que
    /// sustenta o painel de pendências e a regra de não repetir nível.
    /// </para>
    /// <para>
    /// Sem <c>Notifications:Enabled</c> e sem remetente, entra só o log — o comportamento que
    /// valeu até 2026-08-27, e que mantém a rede de segurança utilizável pelo painel.
    /// </para>
    /// </remarks>
    private static void AddNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(NotificationOptions.SectionName);
        services.Configure<NotificationOptions>(section);

        services.AddScoped<LoggingNotificationSender>();

        var options = section.Get<NotificationOptions>() ?? new NotificationOptions();

        if (!options.IsConfigured)
        {
            services.AddScoped<INotificationSender>(sp => sp.GetRequiredService<LoggingNotificationSender>());
            return;
        }

        // Retenta, e pode: `sendMail` não é idempotente, mas o pior caso de uma retentativa é o
        // mesmo aviso duas vezes — barato ao lado de o alerta não sair. O que jamais reaproveita
        // cliente com retry é pagamento.
        services.AddHttpClient(GraphNotificationSender.CLIENT_NAME,
                http => http.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds))
            .AddStandardResilienceHandler();

        // O provedor de token é o mesmo da leitura de caixa, e é singleton pelo cache — pedir um
        // token por aviso faria o Entra ID limitar a taxa da própria autenticação.
        services.TryAddSingleton<GraphTokenProvider>();

        services.AddHttpClient(GraphHttp.TOKEN_CLIENT_NAME,
                http => http.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds))
            .AddStandardResilienceHandler();

        services.AddScoped<GraphNotificationSender>();
        services.AddScoped<INotificationSender, ResilientNotificationSender>();
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

                // O AWSSDK 4 passou a calcular checksum de requisição por padrão
                // (WHEN_SUPPORTED), e com isso manda o corpo do PutObject em `aws-chunked` com
                // trailer de CRC32. O Garage recusa esse formato com "Invalid payload signature"
                // — um 400 que se parece com credencial errada, mas não é: a assinatura do
                // CABEÇALHO passou, e o que ele reprova é a do CORPO. Medido em produção em
                // 2026-09-05, e é a diferença para o PeopleManagement, que grava no mesmo balde
                // com o SDK 3.x e por isso nunca viu o problema.
                RequestChecksumCalculation = Amazon.Runtime.RequestChecksumCalculation.WHEN_REQUIRED,

                // Pelo mesmo motivo, do lado da leitura: o serviço auto-hospedado não devolve os
                // cabeçalhos de checksum que o SDK 4 passou a querer validar.
                ResponseChecksumValidation = Amazon.Runtime.ResponseChecksumValidation.WHEN_REQUIRED,
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
    /// <summary>
    /// Escada de resolução de link — degraus 2 e 3 do doc 09.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>As receitas padrão são as medidas, e só elas.</strong> Em 2026-08-11 quatro
    /// endereços de boleto foram sondados a partir da caixa real: dois entregam documento e dois
    /// exigem portal. Só os dois primeiros viraram receita — configurar um host que não se sabe
    /// responder faria a escada gastar requisição em silêncio e o desfecho parecer falha do
    /// emissor.
    /// </para>
    /// <para>
    /// <strong>O cliente HTTP não segue redirecionamento e não retenta.</strong> Um <c>302</c> é o
    /// jeito mais simples de burlar allowlist; e retentar contra um link expirado só multiplica
    /// requisição partindo da nossa rede para o servidor de outra empresa.
    /// </para>
    /// </remarks>
    private static void AddLinkResolution(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(LinkResolutionOptions.SectionName);
        var options = section.Get<LinkResolutionOptions>() ?? new LinkResolutionOptions();

        // Lista vazia recebe os padrões medidos. Não são default de propriedade porque o binder de
        // configuração mescla coleção por índice: um appsettings com uma receita sobrescreveria a
        // primeira e manteria as demais, produzindo uma allowlist que ninguém escreveu.
        if (options.Recipes.Count == 0)
            options.Recipes = DefaultLinkRecipes();

        if (options.AllowedPorts.Count == 0)
            options.AllowedPorts = [80, 443];

        services.Configure<LinkResolutionOptions>(o =>
        {
            o.Enabled = options.Enabled;
            o.Mode = options.Mode;
            o.TimeoutSeconds = options.TimeoutSeconds;
            o.ReadTimeoutSeconds = options.ReadTimeoutSeconds;
            o.TotalTimeoutSeconds = options.TotalTimeoutSeconds;
            o.MaxBytes = options.MaxBytes;
            o.MaxFetchesPerMessage = options.MaxFetchesPerMessage;
            o.MaxFetchesPerHost = options.MaxFetchesPerHost;
            o.MaxDepth = options.MaxDepth;
            o.MaxFetchesPerSenderPerDay = options.MaxFetchesPerSenderPerDay;
            o.Proxy = options.Proxy;
            o.AllowedPorts = options.AllowedPorts;
            o.BlockedCidrs = options.BlockedCidrs;
            o.AllowedCidrs = options.AllowedCidrs;
            o.BlockedHosts = options.BlockedHosts;
            o.Recipes = options.Recipes;
        });

        // A política é registrada SEMPRE, mesmo com a escada desligada: quem também depende dela
        // é o buscador de comprovante, cuja URL vem do provedor mas é dado de fora do mesmo jeito.
        // Singleton porque compila as faixas UMA vez — e falha alto no arranque se alguma for
        // inválida, em vez de ignorar em silêncio uma faixa que ninguém percebeu que não vale.
        services.AddSingleton<SafeUrlPolicy>();

        // No regime aberto a escada existe sem receita nenhuma — e por isso a condição de desligar
        // deixou de ser "sem receita".
        var hasSomewhereToGo = options.Mode == LinkResolutionMode.Open || options.Recipes.Count > 0;

        if (!options.Enabled || !hasSomewhereToGo)
        {
            services.AddSingleton<IDocumentLinkResolver, NullDocumentLinkResolver>();
            return;
        }

        // O teto por remetente só significa alguma coisa se o contador for o mesmo entre
        // requisições — mesma razão do ExtractionBudget.
        services.AddSingleton<SenderFetchBudget>();

        services.AddHttpClient(HttpDocumentLinkResolver.CLIENT_NAME, http =>
            {
                http.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

                // Identificar-se é o oposto de evadir anti-bot (ADR-012): quem hospeda o documento
                // tem direito de saber quem está buscando e de bloquear se quiser.
                http.DefaultRequestHeaders.UserAgent.ParseAdd("RufinoBillPayment/1.0 (+contas-a-pagar)");
            })
            .ConfigurePrimaryHttpMessageHandler(provider => BuildPinnedHandler(provider))

            // A URL do boleto é credencial ao portador, e os loggers padrão do IHttpClientFactory
            // escrevem a URI COMPLETA em Information — por baixo de todo o cuidado do resolvedor,
            // que só loga o host. Achado M5 da auditoria de 2026-09-03.
            .RemoveAllLoggers();

        services.AddScoped<IDocumentLinkResolver, HttpDocumentLinkResolver>();
    }

    /// <summary>
    /// O handler que disca no IP já conferido, em vez de deixar o nome ser resolvido outra vez.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É isto que fecha o DNS rebinding, e não há como fechar em outro lugar.</strong>
    /// Conferir o host e devolver o NOME para a biblioteca HTTP resolver de novo deixa uma janela
    /// entre a conferência e a conexão — e o DNS que responde nela não é nosso. Com o
    /// <c>ConnectCallback</c>, a segunda resolução deixa de existir.
    /// </para>
    /// <para>
    /// <strong>O TLS continua sendo validado contra o NOME.</strong> O <c>SocketsHttpHandler</c>
    /// faz o handshake depois que este retorno entrega o stream, usando o host da requisição para
    /// SNI e para conferir o certificado — pinar o IP não afrouxa nada disso.
    /// </para>
    /// </remarks>
    private static SocketsHttpHandler BuildPinnedHandler(IServiceProvider provider)
    {
        var policy = provider.GetRequiredService<SafeUrlPolicy>();
        var logger = provider.GetRequiredService<ILogger<SafeUrlPolicy>>();
        var proxy = provider.GetRequiredService<IOptions<LinkResolutionOptions>>().Value.Proxy;

        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = TimeSpan.FromSeconds(10),
        };

        // Com proxy, quem decide o que é alcançável é ELE: a conexão que este processo abre é para
        // o proxy, e conferir o endereço aqui validaria o IP do próprio proxy — interno, portanto
        // recusado, derrubando toda busca. Sem proxy, o IP pinado é a barreira.
        if (!string.IsNullOrWhiteSpace(proxy))
        {
            handler.UseProxy = true;
            handler.Proxy = new WebProxy(proxy);

            logger.LogInformation(
                "Escada de link sai por proxy; a conferência de endereço passa a ser dele, não do processo.");

            return handler;
        }

        handler.UseProxy = false;
        handler.ConnectCallback = async (context, cancellationToken) =>
            {
                var host = context.DnsEndPoint.Host;
                var address = await policy.ResolvePinnedAddressAsync(host, cancellationToken);

                if (address is null)
                {
                    logger.LogWarning(
                        "Endereço de documento recusado: {Host} não resolve para um endereço público permitido.",
                        host);

                    throw new HttpRequestException($"Endereço recusado pela política de rede: {host}");
                }

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

                try
                {
                    await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            };

        return handler;
    }

    /// <summary>
    /// As receitas provadas contra a caixa real em 2026-08-11.
    /// </summary>
    private static List<LinkRecipe> DefaultLinkRecipes() =>
    [
        // SABESP, formato novo: o PDF sai direto, em porta não-padrão. Sondado: 200,
        // application/pdf, 141 KB, sem autenticação.
        new LinkRecipe
        {
            Host = "file-pdf.7az.com.br",
            Port = 7446,
            PathPrefix = "/dx/",
            DirectDocument = true,
        },

        // Asaas: a página da cobrança traz o PDF num `href` próprio. Sondado em 2026-08-26:
        // `www.asaas.com/i/{token}` responde 200 text/html 32 KB sem autenticação, movida por JS —
        // não há linha nem BR Code no HTML —, mas com `href="/b/pdf/{token}"`, que responde 200
        // application/pdf 41 KB e rende DUAS linhas de 47 dígitos e um BR Code. Ou seja: resolve
        // nos dois trilhos, o que mantém ligado o check antifraude PixBarcodeConsistency.
        // O prefixo `/i/` é o que separa a cobrança dos links de rodapé e do rastreador.
        new LinkRecipe
        {
            Host = "www.asaas.com",
            Port = 443,
            PathPrefix = "/i/",
            DirectDocument = false,
        },

        // Acessórias (DTX Sistemas): a página do GED responde 200 text/html 3,7 KB sem
        // autenticação, e o documento está num `<iframe>` escrito por `document.write` — NENHUMA
        // âncora na página inteira. É o caso que motivou a passada bruta do colhedor: a versão que
        // só lia `<a href>` chegava aqui e voltava de mãos vazias. Sondado em 2026-09-10: o iframe
        // aponta para o balde S3 do emissor, que devolve 200 application/pdf 160 KB.
        //
        // O FollowHosts é o subdomínio do balde, não `amazonaws.com`: o genérico autorizaria o
        // balde de qualquer pessoa que tenha uma conta na AWS.
        //
        // ⚠️ A URL do S3 é presignada com `X-Amz-Expires=120` — vale DOIS MINUTOS. Por isso a
        // procedência gravada é a do `/getguia.php`, que é estável, e não a do segundo salto.
        new LinkRecipe
        {
            Host = "app.acessorias.com",
            Port = 443,
            PathPrefix = "/getguia.php",
            DirectDocument = false,
            FollowHosts = ["acessorias.s3.us-east-2.amazonaws.com"],
        },

        // Condomínio (BRCondos): o endereço do boleto responde uma página. Sondado: 200,
        // text/html, 82 KB, sem autenticação. O prefixo /bill/ é o que separa o botão do boleto
        // do link de propaganda que vem logo abaixo dele no mesmo e-mail.
        new LinkRecipe
        {
            Host = "ssl.brcondos.com.br",
            Port = 443,
            PathPrefix = "/bill/",
            DirectDocument = false,
        },
    ];

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

        // Um cliente NOMEADO e SEM chave (2026-08-31): a credencial é por tenant e entra por
        // chamada, resolvida do cofre pelo AsaasClientProvider. O registro deixou de ser
        // condicional — "tenant sem chave" virou caso de dado (Unavailable), não de composição.
        services.AddHttpClient(AsaasHttp.LOOKUP_CLIENT_NAME, ConfigureAsaasClient(options))
            .AddStandardResilienceHandler();

        // O cliente de PAGAMENTO é outro, e a diferença é o que falta: SEM
        // AddStandardResilienceHandler, de propósito. Uma retentativa automática numa submissão
        // é candidata a pagamento duplicado — a retentativa é da fila, que confere por
        // externalReference antes de reenviar (fase 3).
        services.AddHttpClient(AsaasHttp.PAYMENT_CLIENT_NAME, ConfigureAsaasClient(options));

        // O comprovante sai por URL absoluta (capability URL do provedor), então o cliente não
        // tem BaseAddress. Sem retry: a retentativa é da reentrega do outbox, com backoff.
        services.AddHttpClient(HttpPaymentReceiptFetcher.CLIENT_NAME, http =>
            {
                http.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                http.DefaultRequestHeaders.UserAgent.ParseAdd(AsaasOptions.USER_AGENT);
            })

            // A URL do comprovante é capability URL do provedor — credencial ao portador, como a
            // do boleto. Os loggers padrão do IHttpClientFactory a escreveriam completa em
            // Information, contradizendo o "nunca persistida nem logada" do handler (achado M5).
            .RemoveAllLoggers();
        services.AddScoped<IPaymentReceiptFetcher, HttpPaymentReceiptFetcher>();

        // Scoped porque o cofre (ISecretVault) é scoped — vive sobre o DbContext da requisição.
        services.AddScoped<AsaasClientProvider>();
        services.AddScoped<IBillLookupService, AsaasBillLookupService>();
        services.AddScoped<IPixLookupService, AsaasPixLookupService>();
        services.AddScoped<IPaymentAccountVerifier, AsaasAccountVerifier>();
        services.AddScoped<IBillPaymentGateway, AsaasBillPaymentGateway>();
        services.AddScoped<IPixPaymentGateway, AsaasPixPaymentGateway>();
        services.AddScoped<IPaymentWebhookProvisioner, AsaasWebhookProvisioner>();
    }

    private static Action<HttpClient> ConfigureAsaasClient(AsaasOptions options)
        => client =>
        {
            // A barra final importa: sem ela, BaseAddress descarta o segmento "/v3" ao
            // combinar com um caminho relativo.
            var baseUrl = options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/";

            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

            // O provedor recusa a requisição sem User-Agent, e o HttpClient do .NET não manda
            // nenhum por padrão — ver AsaasOptions.USER_AGENT.
            client.DefaultRequestHeaders.UserAgent.ParseAdd(AsaasOptions.USER_AGENT);
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

        // Handlers de Domain Event moram na Application (precisam do mediator) e são registrados
        // lá — Infra → Application seria ciclo. Ver ApplicationDependencies.

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
