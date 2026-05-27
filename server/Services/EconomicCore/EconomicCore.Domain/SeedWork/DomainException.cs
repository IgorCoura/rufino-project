namespace EconomicCore.Domain.SeedWork;

using System.Text.RegularExpressions;

public sealed class DomainException : Exception
{
    // aceita XXX## (ex.: SWK01) e XXX.YYY## (ex.: ECC.EVT04)
    private static readonly Regex ID_PATTERN =
        new(@"^[A-Z]{3}(\.[A-Z]{3})?\d+$", RegexOptions.Compiled);

    public string Id { get; }
    public string MessageTemplate { get; }
    public IReadOnlyList<object> Parameters { get; }
    public string SourcePath { get; }
    public DomainErrorCategory Category { get; }

    public DomainException(
        string id,
        string messageTemplate,
        IReadOnlyList<object> parameters,
        string sourcePath,
        DomainErrorCategory? category = null)
        : base(BuildMessage(messageTemplate, parameters))
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id is required.", nameof(id));
        if (!ID_PATTERN.IsMatch(id))
            throw new ArgumentException($"Id '{id}' does not match pattern XXX## or XXX.YYY##.", nameof(id));
        if (string.IsNullOrWhiteSpace(messageTemplate))
            throw new ArgumentException("MessageTemplate is required.", nameof(messageTemplate));
        if (parameters is null)
            throw new ArgumentNullException(nameof(parameters));
        if (parameters.Any(p => p is null))
            throw new ArgumentException("Parameters cannot contain null values.", nameof(parameters));
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("SourcePath is required.", nameof(sourcePath));

        Id = id;
        MessageTemplate = messageTemplate;
        Parameters = parameters;
        SourcePath = sourcePath;
        Category = category ?? DomainErrorCategory.Validation;
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
