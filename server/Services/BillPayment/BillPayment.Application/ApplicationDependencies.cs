namespace BillPayment.Application;

using BillPayment.Application.Behaviors;
using BillPayment.Application.Bills.Commands;
using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Bills.EventHandlers;
using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.SeedWork;
using BillPayment.Application.Bills;
using BillPayment.Application.Queries;
using BillPayment.Application.Queries.Bills;
using BillPayment.Application.Queries.CaptureItems;
using BillPayment.Application.Expectations.EventHandlers;
using BillPayment.Domain.Expectations;
using BillPayment.Application.Queries.Expectations;
using BillPayment.Application.Queries.Notifications;
using BillPayment.Domain.CaptureItems;
using BillPayment.Application.Queries.CapturedMessages;
using BillPayment.Application.Queries.CaptureSources;
using BillPayment.Application.Queries.Payees;
using BillPayment.Application.Queries.Retention;
using BillPayment.Application.Queries.PayerProfiles;
using BillPayment.Application.Queries.PaymentOrders;
using BillPayment.Application.Queries.TrustedOrigins;
using BillPayment.Application.PaymentOrders.Commands;
using BillPayment.Application.PaymentOrders.EventHandlers;
using BillPayment.Domain.PaymentOrders;
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

        // Quantas vezes insistir num artefato antes de ele virar caso para uma pessoa. Também é
        // regra de negócio: o worker escolhe o ritmo, o negócio escolhe quanto vale insistir.
        services.Configure<CaptureRetryOptions>(configuration.GetSection(CaptureRetryOptions.SectionName));

        // A política inicial de agendamento (ADR-017) e o orçamento da fila de submissão. Regra
        // de negócio configurável, como a de aprovação — a regra em si vive no Domain Service.
        services.Configure<PaymentSchedulingOptions>(
            configuration.GetSection(PaymentSchedulingOptions.SectionName));

        // Mediator próprio (sem MediatR) — escaneia handlers e behaviors do assembly da Application.
        services.AddCustomMediator(typeof(ApplicationDependencies).Assembly);

        // LoggingBehavior é o behavior mais externo e o único ativo. Os commands marcados com
        // IMultiAggregateCommand (seis, listados no CLAUDE.md) seguem sem TransactionBehavior porque há
        // exatamente um SaveEntitiesAsync por handler — a transação implícita do EF cobre tudo.
        // IRequestManager (idempotência) é registrado na Infra, onde vive sua implementação sobre o DbContext.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // Query side (CQRS): interfaces IXxxQueries chamadas direto pelo controller, fora do mediator
        // (padrão eShop, ver EconomicCore). Única exceção autorizada a tocar a Infra.
        // Handlers de Domain Event. Vivem aqui, e não na Infra, porque precisam do mediator e
        // Infra → Application seria ciclo — o dispatcher do outbox os resolve pelo contêiner.
        services.AddScoped<IDomainEventHandler<BillCapturedDomainEvent>, BillCapturedDomainEventHandler>();

        // Expectativa (2.7): o par cumprimento/aprendizado fecha o ciclo, e os três de aviso
        // levam o alerta ao usuário. Todos passam pelo outbox, então precisam ser idempotentes.
        services.AddScoped<IDomainEventHandler<BillValidatedDomainEvent>, FulfillExpectationOnBillValidatedHandler>();
        services.AddScoped<IDomainEventHandler<BillApprovedDomainEvent>, LearnExpectationOnBillApprovedHandler>();
        services.AddScoped<IDomainEventHandler<BillExpectationLearnedDomainEvent>, NotifyExpectationLearnedHandler>();
        services.AddScoped<IDomainEventHandler<BillExpectationCaptureFailedDomainEvent>, NotifyExpectationCaptureFailedHandler>();

        // Quem notifica o escalonamento é o alerta, não a transição para "não cumprido": esta
        // acontece uma vez por ciclo e aquele, quatro. Enquanto o aviso pendurou no evento de
        // Missing, os níveis Warning/Urgent/Overdue eram gravados e nunca chegavam a ninguém.
        services.AddScoped<IDomainEventHandler<BillExpectationAlertRaisedDomainEvent>, NotifyExpectationAlertRaisedHandler>();

        // A ponte captura → expectativa: o artefato que trava marca o ciclo que ele vinha
        // cumprir, e o que destrava solta o ciclo para o painel parar de apontar para ele.
        services.AddScoped<IDomainEventHandler<CaptureItemStuckDomainEvent>, RecordCaptureFailureOnItemStuckHandler>();
        services.AddScoped<IDomainEventHandler<CaptureItemUnstuckDomainEvent>, ClearCaptureFailureOnItemUnstuckHandler>();

        // Fase 3 — o lado do pagamento (ADR-002): a aprovação cria a ordem, a ordem reflete no
        // boleto, e a retenção por vencido avisa. Todos pelo outbox, todos idempotentes.
        services.AddScoped<IDomainEventHandler<BillApprovedDomainEvent>, CreatePaymentOrderOnBillApprovedHandler>();
        services.AddScoped<IDomainEventHandler<BillCancelledDomainEvent>, CancelPaymentOrderOnBillCancelledHandler>();
        services.AddScoped<IDomainEventHandler<PaymentOrderScheduledDomainEvent>, LinkBillOnPaymentOrderScheduledHandler>();
        services.AddScoped<IDomainEventHandler<PaymentOrderPaidDomainEvent>, ReflectPaymentPaidOnBillHandler>();
        services.AddScoped<IDomainEventHandler<PaymentOrderPaidDomainEvent>, CaptureReceiptOnPaymentPaidHandler>();
        services.AddScoped<IDomainEventHandler<PaymentOrderFailedDomainEvent>, ReflectPaymentFailedOnBillHandler>();
        services.AddScoped<IDomainEventHandler<PaymentOrderCancelledDomainEvent>, ReflectPaymentCancelledOnBillHandler>();
        services.AddScoped<IDomainEventHandler<PaymentOrderHeldForConfirmationDomainEvent>, NotifyPaymentAwaitingConfirmationHandler>();
        services.AddScoped<IDomainEventHandler<PaymentOrderRefundedDomainEvent>, NotifyPaymentRefundedHandler>();

        // Quem serve o documento original para uma pessoa. Compartilhado pelas duas leituras
        // que o entregam — item de quarentena e boleto — porque a regra de destravar o PDF
        // cifrado é a mesma nas duas, e duas cópias divergiriam por uma tela só.
        services.AddScoped<UnlockedArtifactReader>();

        services.AddScoped<ITrustedOriginQueries, TrustedOriginQueries>();
        services.AddScoped<IBillQueries, BillQueries>();
        services.AddScoped<IBillReadingWorkQueries, BillReadingWorkQueries>();

        // Compartilhado pela fila de análise e pelo pedido manual de reler — duas cópias
        // divergiriam, e a divergência apareceria como "pela fila lê, pelo botão não".
        services.AddScoped<IBillReadingSource, BillReadingSource>();
        services.AddScoped<IPayeeQueries, PayeeQueries>();
        services.AddScoped<IPayerProfileQueries, PayerProfileQueries>();
        services.AddScoped<ICaptureSourceQueries, CaptureSourceQueries>();
        services.AddScoped<ICaptureItemQueries, CaptureItemQueries>();
        services.AddScoped<ICaptureItemWorkQueries, CaptureItemWorkQueries>();
        services.AddScoped<IBillExpectationQueries, BillExpectationQueries>();
        services.AddScoped<ITenantNotificationQueries, TenantNotificationQueries>();
        services.AddScoped<ICapturedMessageQueries, CapturedMessageQueries>();
        services.AddScoped<ICaptureRetentionQueries, CaptureRetentionQueries>();
        services.AddScoped<IPaymentQueries, PaymentQueries>();
        services.AddScoped<IPaymentOrderWorkQueries, PaymentOrderWorkQueries>();

        return services;
    }
}
