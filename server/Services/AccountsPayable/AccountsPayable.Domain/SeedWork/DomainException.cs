namespace AccountsPayable.Domain.SeedWork;

using System.Text.RegularExpressions;

/// <summary>
/// Single, canonical domain exception class. Aggregates NEVER instantiate this directly —
/// always via a factory in <c>&lt;Aggregate&gt;Errors.cs</c>.
/// <para>
/// Error Id format for this Bounded Context:
/// <list type="bullet">
///   <item><c>AP.&lt;AGG&gt;##</c> — aggregate-specific (e.g., <c>AP.PAY01</c>)</item>
///   <item><c>AP##</c> — cross-aggregate BC errors (e.g., <c>AP01</c>)</item>
///   <item><c>SWK##</c> — SeedWork base-class errors (e.g., <c>SWK01</c>)</item>
/// </list>
/// The regex below accepts 2-4 letter prefixes (segment) to match the project's <c>AP</c> convention,
/// while still allowing 3-letter segments (e.g., <c>SWK</c>, <c>PAY</c>).
/// </para>
/// </summary>
public sealed class DomainException : Exception
{
    private static readonly Regex ID_PATTERN =
        new(@"^[A-Z]{2,4}(\.[A-Z]{2,4})?\d+$", RegexOptions.Compiled);

    public string Id { get; }
    public string MessageTemplate { get; }
    public IReadOnlyList<object> Parameters { get; }
    public string SourcePath { get; }

    public DomainException(
        string id,
        string messageTemplate,
        IReadOnlyList<object> parameters,
        string sourcePath)
        : base(BuildMessage(messageTemplate, parameters))
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id is required.", nameof(id));
        if (!ID_PATTERN.IsMatch(id))
            throw new ArgumentException(
                $"Id '{id}' does not match pattern XX## / XXX## / XX.YYY## / XXX.YYY##.",
                nameof(id));
        if (string.IsNullOrWhiteSpace(messageTemplate))
            throw new ArgumentException("MessageTemplate is required.", nameof(messageTemplate));
        ArgumentNullException.ThrowIfNull(parameters);
        if (parameters.Any(p => p is null))
            throw new ArgumentException("Parameters cannot contain null values.", nameof(parameters));
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("SourcePath is required.", nameof(sourcePath));

        Id = id;
        MessageTemplate = messageTemplate;
        Parameters = parameters;
        SourcePath = sourcePath;
    }

    private static string BuildMessage(string template, IReadOnlyList<object> parameters)
    {
        if (string.IsNullOrWhiteSpace(template))
            return template ?? string.Empty;
        if (parameters is null || parameters.Count == 0)
            return template;
        return string.Format(template, parameters.ToArray());
    }

    public override string ToString() => $"[{Id}] {Message} | at {SourcePath}";
}
