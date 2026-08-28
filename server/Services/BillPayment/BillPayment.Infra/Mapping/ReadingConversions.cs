namespace BillPayment.Infra.Mapping;

using System.Text.Json;
using BillPayment.Domain.Bills;
using BillPayment.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// O retrato da leitura por IA em <c>jsonb</c> — mesmo racional do <see cref="LookupConversions"/>:
/// não é owned type porque contém <c>TaxId</c> e <c>CompetencePeriod</c>, e owned de 2º nível
/// anexado a agregado já persistido grava NULL. A reidratação passa por
/// <c>DocumentReading.Rehydrate</c> com os VOs reconstruídos pelas factories públicas — valor
/// corrompido falha alto na leitura.
/// </summary>
internal static class ReadingConversions
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static readonly ValueConverter<DocumentReading?, string?> Reading =
        new(reading => Serialize(reading), json => Deserialize(json));

    public static readonly ValueComparer<DocumentReading?> ReadingComparer =
        new((left, right) => left == null ? right == null : left.Equals(right),
            reading => reading == null ? 0 : reading.GetHashCode(),
            reading => reading);

    private static string? Serialize(DocumentReading? reading)
        => reading is null
            ? null
            : JsonSerializer.Serialize(
                new ReadingRecord(
                    reading.PayerName,
                    reading.PayerTaxId?.Value,
                    reading.PayeeName,
                    reading.PayeeTaxId?.Value,
                    reading.AccountReference,
                    reading.Amount,
                    reading.DueDate,
                    reading.BillingPeriodText,
                    reading.Competence?.Year,
                    reading.Competence?.Month,
                    reading.Description,
                    reading.Notes,
                    reading.ReadAt),
                Json);

    private static DocumentReading? Deserialize(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        var record = JsonSerializer.Deserialize<ReadingRecord>(json, Json)
            ?? throw new InvalidOperationException("Retrato de leitura por IA ilegível.");

        return DocumentReading.Rehydrate(
            record.PayerName,
            record.PayerTaxId is null ? null : TaxId.Parse(record.PayerTaxId),
            record.PayeeName,
            record.PayeeTaxId is null ? null : TaxId.Parse(record.PayeeTaxId),
            record.AccountReference,
            record.Amount,
            record.DueDate,
            record.BillingPeriodText,
            record.CompetenceYear is { } year && record.CompetenceMonth is { } month
                ? new CompetencePeriod(year, month)
                : null,
            record.Description,
            record.Notes,
            record.ReadAt);
    }

    private sealed record ReadingRecord(
        string? PayerName,
        string? PayerTaxId,
        string? PayeeName,
        string? PayeeTaxId,
        string? AccountReference,
        decimal? Amount,
        DateOnly? DueDate,
        string? BillingPeriodText,
        int? CompetenceYear,
        int? CompetenceMonth,
        string? Description,
        string? Notes,
        DateTimeOffset ReadAt);
}
