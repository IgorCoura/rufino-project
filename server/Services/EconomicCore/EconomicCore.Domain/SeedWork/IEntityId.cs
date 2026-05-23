namespace EconomicCore.Domain.SeedWork;

/// <summary>
/// Contrato comum para todos os strongly-typed Ids do domínio.
/// Cada Aggregate/Entity define seu próprio record struct (EconomicEventId, etc.)
/// que implementa este contrato. Permite que Entity&lt;TId&gt; construa novos Ids sem reflexão.
/// </summary>
public interface IEntityId<TSelf> where TSelf : struct, IEntityId<TSelf>
{
    Guid Value { get; }
    static abstract TSelf New();
    static abstract TSelf From(Guid value);
    static abstract TSelf Empty { get; }
}
