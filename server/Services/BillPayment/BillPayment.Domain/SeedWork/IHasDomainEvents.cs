namespace BillPayment.Domain.SeedWork;

/// <summary>
/// A base NÃO-GENÉRICA de todo Aggregate Root que acumula eventos.
/// </summary>
/// <remarks>
/// <para>
/// Existe por um motivo só, e ele é caro: <c>AggregateRoot&lt;TId&gt;</c> é genérico, e sem esta
/// interface o dreno do <c>SaveEntitiesAsync</c> precisava de um <c>case</c> por agregado —
/// lista que só se mantinha correta pela memória de quem editava. Em 2026-09-08 o
/// <c>PayerProfile</c> ficou de fora dela e o <c>AsaasAccountLinkedDomainEvent</c> passou a ser
/// descartado em silêncio: nenhum webhook foi provisionado por um mês, sem um único erro.
/// </para>
/// <para>
/// Com a interface, o dreno pergunta <c>is IHasDomainEvents</c> e agregado novo entra sozinho.
/// <strong>Não acrescente uma lista de tipos em lugar nenhum</strong> — foi exatamente isso que
/// falhou.
/// </para>
/// </remarks>
public interface IHasDomainEvents
{
    /// <summary>Retira os eventos acumulados, esvaziando o agregado. Chamado pelo Unit of Work.</summary>
    IReadOnlyList<IDomainEvent> PullDomainEvents();
}
