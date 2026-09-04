namespace BillPayment.Domain.Expectations;

using BillPayment.Domain.SeedWork;

/// <summary>Um alerta já enviado para um ciclo, com o nível e o instante.</summary>
/// <remarks>
/// <strong>É registro, não log.</strong> É dele que sai a garantia de não repetir nível — e
/// mantê-lo no agregado, e não no canal de envio, é o que faz a regra sobreviver a troca de
/// canal: e-mail hoje, outro amanhã, e a contagem continua a mesma.
/// </remarks>
public sealed class AlertRecord : ValueObject
{
    public AlertLevel Level { get; }
    public DateTime SentAt { get; }

    private AlertRecord(AlertLevel level, DateTime sentAt)
    {
        Level = level;
        SentAt = sentAt;
    }

    public static AlertRecord Of(AlertLevel level, DateTime sentAt)
        => level is null ? throw BillExpectationErrors.AlertLevelRequired() : new AlertRecord(level, sentAt);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Level;
        yield return SentAt;
    }
}
