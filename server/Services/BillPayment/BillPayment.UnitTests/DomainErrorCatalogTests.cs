namespace BillPayment.UnitTests;

using System.Reflection;
using System.Text.RegularExpressions;
using BillPayment.Domain.SeedWork;

/// <summary>
/// O catálogo inteiro de erros do Domain, por reflexão: toda factory produz um id no formato do
/// BC, com mensagem, e nenhum id é reutilizado por duas factories.
/// </summary>
/// <remarks>
/// Existe porque a auditoria de 2026-08-28 achou 52 factories que nenhum teste chamava — e uma
/// delas partilhava o id <c>BLP.BIL05</c> com outra. Este teste não substitui o teste da
/// invariante que a lança; garante o mínimo que um catálogo precisa ter para a UI traduzir por id.
/// </remarks>
public sealed partial class DomainErrorCatalogTests
{
    private static readonly DateTime FixedInstant = new(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);

    // Toda factory de erro devolve DomainException com id no padrão do BC e mensagem preenchida.
    [Fact]
    public void EveryErrorFactory_ShouldProduceAWellFormedDomainException()
    {
        var problems = new List<string>();

        foreach (var (type, method) in ErrorFactories())
        {
            var exception = Invoke(type, method, problems);
            if (exception is null)
                continue;

            if (!ErrorId().IsMatch(exception.Id))
                problems.Add($"{type.Name}.{method.Name}: id '{exception.Id}' fora do padrão.");

            if (string.IsNullOrWhiteSpace(exception.Message))
                problems.Add($"{type.Name}.{method.Name}: mensagem vazia.");
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    // Nenhum id é partilhado por duas factories — a UI traduz por id, e um id repetido faz duas
    // recusas diferentes aparecerem com o mesmo texto.
    [Fact]
    public void EveryErrorId_ShouldBelongToExactlyOneFactory()
    {
        var owners = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var (type, method) in ErrorFactories())
        {
            var exception = Invoke(type, method, []);
            if (exception is null)
                continue;

            if (!owners.TryGetValue(exception.Id, out var list))
                owners[exception.Id] = list = [];

            list.Add($"{type.Name}.{method.Name}");
        }

        var duplicated = owners.Where(o => o.Value.Count > 1)
            .Select(o => $"{o.Key}: {string.Join(", ", o.Value)}")
            .ToList();

        Assert.True(duplicated.Count == 0, string.Join(Environment.NewLine, duplicated));
    }

    private static IEnumerable<(Type Type, MethodInfo Method)> ErrorFactories()
        => typeof(DomainException).Assembly
            .GetTypes()
            .Where(t => t.IsClass && t.IsAbstract && t.IsSealed && t.Name.EndsWith("Errors", StringComparison.Ordinal))
            .SelectMany(t => t
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => m.ReturnType == typeof(DomainException))
                .Select(m => (t, m)));

    private static DomainException? Invoke(Type type, MethodInfo method, List<string> problems)
    {
        try
        {
            var arguments = method.GetParameters().Select(ArgumentFor).ToArray();
            return (DomainException?)method.Invoke(null, arguments);
        }
        catch (TargetInvocationException ex)
        {
            problems.Add($"{type.Name}.{method.Name}: lançou {ex.InnerException?.GetType().Name} — {ex.InnerException?.Message}");
            return null;
        }
    }

    private static object? ArgumentFor(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue)
            return parameter.DefaultValue;

        var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;

        if (type == typeof(string)) return "x";
        if (type == typeof(Guid)) return Guid.Empty;
        if (type == typeof(int)) return 1;
        if (type == typeof(long)) return 1L;
        if (type == typeof(decimal)) return 1m;
        if (type == typeof(double)) return 1d;
        if (type == typeof(bool)) return false;
        if (type == typeof(DateOnly)) return DateOnly.FromDateTime(FixedInstant);
        if (type == typeof(DateTime)) return FixedInstant;
        if (type == typeof(DateTimeOffset)) return new DateTimeOffset(FixedInstant);
        if (type == typeof(TimeSpan)) return TimeSpan.FromMinutes(1);
        if (typeof(IEnumerable<string>).IsAssignableFrom(type)) return new[] { "x" };

        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    [GeneratedRegex(@"^(SWK\d{2}|SHK\.[A-Z]{3}\d{2}|BLP\d{2}|BLP\.[A-Z]{3}\d{2})$")]
    private static partial Regex ErrorId();
}
