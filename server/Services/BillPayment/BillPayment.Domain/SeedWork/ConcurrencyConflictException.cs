namespace BillPayment.Domain.SeedWork;

/// <summary>
/// Outra operação gravou o mesmo agregado entre a leitura e a gravação desta.
/// </summary>
/// <remarks>
/// <para>
/// Não é <see cref="DomainException"/>: nenhuma regra de negócio foi violada — a decisão foi
/// tomada sobre um estado que já não existe. Quem chamou precisa recarregar e decidir de novo;
/// a API traduz para 409.
/// </para>
/// <para>
/// A Infra lança esta exceção a partir do token de concorrência do banco (o <c>xmin</c> do
/// Postgres em <c>bills</c>, <c>capture_items</c>, <c>capture_sources</c> e
/// <c>bill_expectations</c>). Até 2026-08-28 não havia token nenhum: dois aprovadores
/// simultâneos liam <c>AwaitingApproval</c>, ambos gravavam <c>Approved</c> e dois eventos de
/// aprovação entravam no outbox — na fase de pagamento, dois pagamentos.
/// </para>
/// </remarks>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
        : base("Este registro foi alterado por outra operação. Recarregue e tente de novo.")
    {
    }

    public ConcurrencyConflictException(string message) : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
