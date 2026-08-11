namespace BillPayment.Application;

using BillPayment.Application.Behaviors;
using BillPayment.Application.Bills.Commands;
using BillPayment.Application.Bills.EventHandlers;
using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.SeedWork;
using BillPayment.Application.Queries.Bills;
using BillPayment.Application.Queries.CaptureItems;
using BillPayment.Application.Queries.CaptureSources;
using BillPayment.Application.Queries.Payees;
using BillPayment.Application.Queries.PayerProfiles;
using BillPayment.Application.Queries.TrustedOrigins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ApplicationDependencies
{
    public static IServiceCollection AddApplicationDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Política de aprovação: prazo de validade do retrato e teto de valor. É regra de
        // negócio configurável, não infraestrutura — por isso é registrada aqui.
        services.Configure<ApprovalOptions>(configuration.GetSection(ApprovalOptions.SectionName));

        // Mediator próprio (sem MediatR) — escaneia handlers e behaviors do assembly da Application.
        services.AddCustomMediator(typeof(ApplicationDependencies).Assembly);

        // LoggingBehavior é o behavior mais externo e o único ativo nesta fase. RegisterLatePaymentEvent
        // é o único command multi-aggregate (marker IMultiAggregateCommand) e segue sem TransactionBehavior
        // porque há exatamente um SaveEntitiesAsync por handler — a transação implícita do EF cobre tudo.
        // IRequestManager (idempotência) é registrado na Infra, onde vive sua implementação sobre o DbContext.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // Query side (CQRS): interfaces IXxxQueries chamadas direto pelo controller, fora do mediator
        // (padrão eShop, ver EconomicCore). Única exceção autorizada a tocar a Infra.
        // Handlers de Domain Event. Vivem aqui, e não na Infra, porque precisam do mediator e
        // Infra → Application seria ciclo — o dispatcher do outbox os resolve pelo contêiner.
        services.AddScoped<IDomainEventHandler<BillCapturedDomainEvent>, BillCapturedDomainEventHandler>();

        services.AddScoped<ITrustedOriginQueries, TrustedOriginQueries>();
        services.AddScoped<IBillQueries, BillQueries>();
        services.AddScoped<IPayeeQueries, PayeeQueries>();
        services.AddScoped<IPayerProfileQueries, PayerProfileQueries>();
        services.AddScoped<ICaptureSourceQueries, CaptureSourceQueries>();
        services.AddScoped<ICaptureItemQueries, CaptureItemQueries>();

        return services;
    }
}
