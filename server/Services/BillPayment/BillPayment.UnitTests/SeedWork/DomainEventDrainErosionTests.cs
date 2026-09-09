namespace BillPayment.UnitTests.SeedWork;

using System.Reflection;
using BillPayment.Domain.SeedWork;

/// <summary>
/// A guarda contra a falha que custou um mês de webhooks: evento emitido por um agregado que o
/// dreno do <c>SaveEntitiesAsync</c> não enxerga.
/// </summary>
/// <remarks>
/// <para>
/// Até 2026-09-09 o dreno era um <c>switch</c> com um <c>case</c> por Aggregate Root, mantido
/// correto só pela memória de quem editava. O <c>PayerProfile</c> ficou de fora, o
/// <c>AsaasAccountLinkedDomainEvent</c> passou a ser descartado no fim do escopo e nenhum webhook
/// foi provisionado — sem erro, sem log, sem teste vermelho.
/// </para>
/// <para>
/// A correção estrutural foi a interface não-genérica <see cref="IHasDomainEvents"/>: o dreno
/// pergunta o tipo em vez de consultar uma lista. Estes testes existem para que a lista não volte
/// por outra porta — um agregado que acumule evento sem herdar de <c>AggregateRoot&lt;TId&gt;</c>
/// seria invisível do mesmo jeito.
/// </para>
/// </remarks>
public sealed class DomainEventDrainErosionTests
{
    private static readonly Assembly DomainAssembly = typeof(IHasDomainEvents).Assembly;

    // Todo Aggregate Root concreto tem de ser drenável pela interface. Hoje isso vem de graça da
    // base, e é justamente essa gratuidade que o teste protege.
    [Fact]
    public void EveryConcreteAggregateRoot_ShouldBeDrainableThroughTheNonGenericInterface()
    {
        var offenders = ConcreteAggregateRoots()
            .Where(t => !typeof(IHasDomainEvents).IsAssignableFrom(t))
            .Select(t => t.FullName!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "Estes Aggregate Roots não são drenados pelo Unit of Work e emitiriam eventos que "
            + "ninguém publica, em silêncio: " + string.Join(", ", offenders));
    }

    // A outra porta: uma classe que guarde eventos por conta própria, fora da base. O dreno só
    // conhece IHasDomainEvents — quem acumular evento sem implementá-la some do mesmo jeito.
    [Fact]
    public void NoTypeShouldHoldDomainEventsOutsideTheDrainableBase()
    {
        var offenders = DomainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(HoldsDomainEvents)
            .Where(t => !typeof(IHasDomainEvents).IsAssignableFrom(t))
            .Select(t => t.FullName!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "Estes tipos acumulam Domain Events mas não implementam IHasDomainEvents, então o "
            + "dreno do SaveEntitiesAsync não os enxerga: " + string.Join(", ", offenders));
    }

    // O agregado do incidente, nomeado. Um teste genérico que passasse por engano ainda deixaria
    // este vermelho — e é este o caso que ninguém pode reintroduzir.
    [Fact]
    public void PayerProfile_ShouldBeDrainable()
    {
        var payerProfile = DomainAssembly.GetType("BillPayment.Domain.PayerProfiles.PayerProfile");

        Assert.NotNull(payerProfile);
        Assert.True(
            typeof(IHasDomainEvents).IsAssignableFrom(payerProfile),
            "O PayerProfile emite AsaasAccountLinkedDomainEvent; fora do dreno, o webhook do "
            + "tenant nunca é provisionado e nada avisa.");
    }

    private static IEnumerable<Type> ConcreteAggregateRoots()
        => DomainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && InheritsAggregateRoot(t));

    private static bool InheritsAggregateRoot(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
                return true;
        }

        return false;
    }

    /// <summary>Tem campo ou propriedade que guarda <see cref="IDomainEvent"/>, a qualquer visibilidade.</summary>
    private static bool HoldsDomainEvents(Type type)
    {
        const BindingFlags FLAGS =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        return type.GetFields(FLAGS).Any(f => IsDomainEventCollection(f.FieldType))
            || type.GetProperties(FLAGS).Any(p => IsDomainEventCollection(p.PropertyType));
    }

    private static bool IsDomainEventCollection(Type type)
        => type.IsGenericType
            && type.GetGenericArguments().Any(arg => typeof(IDomainEvent).IsAssignableFrom(arg));
}
