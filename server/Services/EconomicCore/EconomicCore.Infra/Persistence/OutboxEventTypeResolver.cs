namespace EconomicCore.Infra.Persistence;

using System.Text.Json;
using EconomicCore.Domain.SeedWork;

public interface IOutboxEventTypeResolver
{
    IDomainEvent Resolve(OutboxMessage message);
}

internal sealed class OutboxEventTypeResolver : IOutboxEventTypeResolver
{
    private readonly Dictionary<string, Type> _typesByFullName;

    public OutboxEventTypeResolver()
    {
        var contract = typeof(IDomainEvent);

        // Built once at startup (singleton) — indexes every concrete IDomainEvent in the Domain assembly by FullName.
        // Acts as fail-fast: an outbox row whose EventType no longer maps to a CLR type surfaces as a clear error.
        _typesByFullName = contract.Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && contract.IsAssignableFrom(t) && t.FullName is not null)
            .ToDictionary(t => t.FullName!, t => t);
    }

    public IDomainEvent Resolve(OutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!_typesByFullName.TryGetValue(message.EventType, out var type))
            throw new InvalidOperationException($"Tipo de evento de outbox desconhecido: '{message.EventType}'.");

        if (JsonSerializer.Deserialize(message.Payload, type) is not IDomainEvent domainEvent)
            throw new InvalidOperationException($"Falha ao desserializar o payload do outbox para '{message.EventType}'.");

        return domainEvent;
    }
}
