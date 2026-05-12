namespace AccountsPayable.Domain.Payables.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Reference to where the payment proof lives (URI) + the kind of document. Minimal Sprint 2
/// shape: a strict absolute URI and a typed enum. Storage / signing / lifecycle is Infra concern.
/// </summary>
public sealed class PaymentProof : ValueObject
{
    public const int URI_MAX_LENGTH = 2048;

    public string Uri { get; }
    public PaymentProofType Type { get; }

    public PaymentProof(string uri, PaymentProofType type)
    {
        if (type is null)
            throw PaymentProofErrors.TypeRequired();
        if (string.IsNullOrWhiteSpace(uri))
            throw PaymentProofErrors.UriRequired();

        var trimmed = uri.Trim();
        if (trimmed.Length > URI_MAX_LENGTH)
            throw PaymentProofErrors.UriTooLong(URI_MAX_LENGTH);
        if (!System.Uri.TryCreate(trimmed, UriKind.Absolute, out _))
            throw PaymentProofErrors.InvalidUri(trimmed);

        Uri = trimmed;
        Type = type;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Uri;
        yield return Type;
    }
}
