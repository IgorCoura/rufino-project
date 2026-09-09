namespace BillPayment.UnitTests.SeedWork;

using System.Reflection;
using System.Text.Json;
using BillPayment.Domain.SeedWork;

/// <summary>
/// A guarda contra o evento que o outbox aceita gravar e não consegue ler de volta.
/// </summary>
/// <remarks>
/// <para>
/// O dreno serializa o evento em JSON e o processador o reconstrói do outro lado. A gravação
/// aceita qualquer coisa — <c>JsonSerializer.Serialize</c> escreve um <c>Enumeration</c> como
/// objeto sem reclamar —, e a recusa só aparece na leitura, dentro do worker, como
/// <c>NotSupportedException: Deserialization of types without a parameterless constructor…</c>.
/// A mensagem já chega tarde: o efeito falhou, o outbox conta a tentativa, e cinco tentativas
/// depois a mensagem vai para dead-letter.
/// </para>
/// <para>
/// Foi o que aconteceu com o <c>PaymentOrderCancelledDomainEvent</c>, que carregava
/// <c>BillActionOrigin</c> desde 2026-09-08: TODO cancelamento de ordem morria na fila, o espelho
/// nunca disparava, e o boleto ficava com a data e o vínculo de um pagamento que não ia
/// acontecer. Em silêncio — o outbox persistia a exceção na coluna <c>error</c> e não a logava.
/// </para>
/// <para>
/// A regra é a mesma do resto do BC: Smart Enum viaja pelo <strong>nome</strong>, e quem consome
/// o traduz de volta. Este teste existe para que ela não dependa de alguém lembrar.
/// </para>
/// </remarks>
public sealed class DomainEventSerializationErosionTests
{
    private static readonly Assembly DomainAssembly = typeof(IDomainEvent).Assembly;

    // Nenhuma propriedade de evento pode ser Smart Enum: o desserializador não sabe construí-lo.
    [Fact]
    public void NoDomainEventShouldCarryASmartEnum()
    {
        var offenders = DomainEvents()
            .SelectMany(e => Properties(e).Select(p => (Event: e, Property: p)))
            .Where(x => CarriesEnumeration(x.Property.PropertyType))
            .Select(x => $"{x.Event.Name}.{x.Property.Name} ({Describe(x.Property.PropertyType)})")
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "Estes eventos carregam Smart Enum e o outbox não consegue reconstruí-los — o efeito "
            + "morre na fila e vai para dead-letter, em silêncio. Faça o campo viajar como o Name "
            + "(string) e traduza no consumidor: " + string.Join(", ", offenders));
    }

    // O contrato inteiro, provado como o outbox o exercita: pedir ao desserializador que
    // construa cada tipo que viaja no evento. Pega o que a regra do Smart Enum não alcança —
    // qualquer tipo sem construtor que ele saiba usar.
    [Fact]
    public void EveryTypeCarriedByADomainEventShouldBeReconstructible()
    {
        var offenders = new List<string>();

        foreach (var domainEvent in DomainEvents())
        {
            foreach (var carried in Properties(domainEvent).SelectMany(p => Unwrap(p.PropertyType)).Distinct())
            {
                try
                {
                    // O corpo é irrelevante: a recusa acontece ao montar o conversor, antes de
                    // olhar um campo. Falta de membro obrigatório sai como JsonException, que é
                    // outro assunto e não entra aqui.
                    _ = JsonSerializer.Deserialize("{}", carried);
                }
                catch (NotSupportedException)
                {
                    offenders.Add($"{domainEvent.Name} carrega {carried.Name}");
                }
                catch (JsonException)
                {
                    // O conversor existe; só faltou dado. É o desfecho bom.
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "O desserializador do outbox não sabe reconstruir estes tipos, então o evento morre "
            + "na fila e vai para dead-letter: " + string.Join(", ", offenders.Distinct()));
    }

    // O evento do incidente, nomeado — um teste genérico que passasse por engano deixaria este
    // vermelho, e é este o caso que não pode voltar.
    [Fact]
    public void PaymentOrderCancelled_ShouldCarryTheOriginAsAName()
    {
        var domainEvent = DomainAssembly.GetType(
            "BillPayment.Domain.PaymentOrders.PaymentOrderCancelledDomainEvent");

        Assert.NotNull(domainEvent);
        Assert.Equal(typeof(string), domainEvent.GetProperty("Origin")!.PropertyType);
    }

    private static IEnumerable<Type> DomainEvents()
        => DomainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IDomainEvent).IsAssignableFrom(t));

    private static PropertyInfo[] Properties(Type type)
        => type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

    /// <summary>O próprio tipo, ou o que ele embrulha (nulo, coleção), deriva de <c>Enumeration</c>.</summary>
    private static bool CarriesEnumeration(Type type)
        => typeof(Enumeration).IsAssignableFrom(type)
            || (type.IsGenericType && type.GetGenericArguments().Any(CarriesEnumeration));

    private static string Describe(Type type)
        => type.IsGenericType
            ? $"{type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)]}<{string.Join(", ", type.GetGenericArguments().Select(Describe))}>"
            : type.Name;

    /// <summary>Os tipos que de fato viajam: o próprio, o que ele embrulha, o que ele contém.</summary>
    private static IEnumerable<Type> Unwrap(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
        {
            yield return underlying;
            yield break;
        }

        yield return type;

        if (!type.IsGenericType)
            yield break;

        foreach (var argument in type.GetGenericArguments().SelectMany(Unwrap))
            yield return argument;
    }
}
