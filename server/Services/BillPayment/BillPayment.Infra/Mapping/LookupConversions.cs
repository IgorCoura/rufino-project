namespace BillPayment.Infra.Mapping;

using System.Text.Json;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Retratos de consulta oficial em <c>jsonb</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Não são owned types.</strong> <c>LookupSnapshot</c> contém <c>Money</c> e
/// <c>LookupParty</c> (que por sua vez contém <c>TaxId</c>) — owned de 2º nível anexado a
/// agregado já persistido não é rastreado pelo EF e grava NULL. E é exatamente esse o cenário
/// aqui: a revalidação anexa um retrato novo a um <c>Bill</c> carregado do banco.
/// </para>
/// <para>
/// <strong>A desserialização passa pelas factories públicas do domínio</strong>, nunca por
/// construtor privado: um valor corrompido no banco falha alto na leitura em vez de virar
/// retrato inválido circulando pela verificação.
/// </para>
/// </remarks>
internal static class LookupConversions
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static readonly ValueConverter<LookupSnapshot?, string?> BankSlip =
        new(snapshot => Serialize(snapshot), json => DeserializeBankSlip(json));

    public static readonly ValueComparer<LookupSnapshot?> BankSlipComparer =
        new((left, right) => left == null ? right == null : left.Equals(right),
            snapshot => snapshot == null ? 0 : snapshot.GetHashCode(),
            snapshot => snapshot);

    public static readonly ValueConverter<PixLookupSnapshot?, string?> Pix =
        new(snapshot => Serialize(snapshot), json => DeserializePix(json));

    public static readonly ValueComparer<PixLookupSnapshot?> PixComparer =
        new((left, right) => left == null ? right == null : left.Equals(right),
            snapshot => snapshot == null ? 0 : snapshot.GetHashCode(),
            snapshot => snapshot);

    public static readonly ValueConverter<IReadOnlyList<BillLookupRecord>, string> History =
        new(records => SerializeHistory(records), json => DeserializeHistory(json));

    public static readonly ValueComparer<IReadOnlyList<BillLookupRecord>> HistoryComparer =
        new((left, right) => left!.SequenceEqual(right!),
            records => records.Aggregate(0, (hash, r) => HashCode.Combine(hash, r.GetHashCode())),
            records => records.ToList());

    private static string? Serialize(LookupSnapshot? snapshot)
        => snapshot is null ? null : JsonSerializer.Serialize(ToRecord(snapshot), Json);

    private static string? Serialize(PixLookupSnapshot? snapshot)
        => snapshot is null ? null : JsonSerializer.Serialize(ToRecord(snapshot), Json);

    private static LookupSnapshot? DeserializeBankSlip(string? json)
        => string.IsNullOrWhiteSpace(json)
            ? null
            : FromRecord(JsonSerializer.Deserialize<BankSlipRecord>(json, Json)
                ?? throw new InvalidOperationException("Retrato da consulta de boleto ilegível."));

    private static PixLookupSnapshot? DeserializePix(string? json)
        => string.IsNullOrWhiteSpace(json)
            ? null
            : FromRecord(JsonSerializer.Deserialize<PixRecord>(json, Json)
                ?? throw new InvalidOperationException("Retrato do decode Pix ilegível."));

    private static string SerializeHistory(IReadOnlyList<BillLookupRecord> records)
        => JsonSerializer.Serialize(
            records.Select(r => new HistoryRecord(
                r.Rail.Id,
                r.Status.Id,
                r.BankSlipSnapshot is null ? null : ToRecord(r.BankSlipSnapshot),
                r.PixSnapshot is null ? null : ToRecord(r.PixSnapshot),
                r.ReasonCode,
                r.AttemptedAt)).ToList(),
            Json);

    private static List<BillLookupRecord> DeserializeHistory(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        var records = JsonSerializer.Deserialize<List<HistoryRecord>>(json, Json)
            ?? throw new InvalidOperationException("Histórico de consultas ilegível.");

        return records.ConvertAll(Rehydrate);
    }

    // Remonta o resultado pelas factories públicas e devolve-o ao domínio, em vez de uma
    // factory de persistência capaz de fabricar histórico incoerente — retrato sem status
    // resolvido, ou resolvido sem retrato.
    private static BillLookupRecord Rehydrate(HistoryRecord r)
    {
        var status = Enumeration.FromValue<LookupStatus>(r.Status);
        var reason = r.ReasonCode ?? status.Name;

        if (Enumeration.FromValue<PaymentRail>(r.Rail) == PaymentRail.Boleto)
        {
            if (status == LookupStatus.Resolved)
                return BillLookupRecord.ForBankSlip(
                    BillLookupResult.Resolved(FromRecord(r.BankSlipSnapshot!), r.AttemptedAt));

            return BillLookupRecord.ForBankSlip(status == LookupStatus.Unavailable
                ? BillLookupResult.Unavailable(reason, null, r.AttemptedAt)
                : BillLookupResult.Unresolved(reason, null, r.AttemptedAt));
        }

        if (status == LookupStatus.Resolved)
            return BillLookupRecord.ForPix(
                PixLookupResult.Resolved(FromRecord(r.PixSnapshot!), r.AttemptedAt));

        return BillLookupRecord.ForPix(status == LookupStatus.Unavailable
            ? PixLookupResult.Unavailable(reason, null, r.AttemptedAt)
            : PixLookupResult.Unresolved(reason, null, r.AttemptedAt));
    }

    private static BankSlipRecord ToRecord(LookupSnapshot s)
        => new(
            ToRecord(s.Beneficiary),
            s.BankCode?.Value,
            s.Amount?.Amount,
            s.OriginalAmount?.Amount,
            s.Interest?.Amount,
            s.Fine?.Amount,
            s.Discount?.Amount,
            s.MinAmount?.Amount,
            s.MaxAmount?.Amount,
            s.AllowChangeValue,
            s.DueDate,
            s.IsOverdue,
            s.Fee?.Amount,
            s.MinimumScheduleDate,
            s.ConsultedAt);

    private static LookupSnapshot FromRecord(BankSlipRecord r)
        => LookupSnapshot.Create(
            FromRecord(r.Beneficiary),
            r.ConsultedAt,
            bankCode: r.BankCode is null ? null : new BankCode(r.BankCode),
            amount: Money(r.Amount),
            originalAmount: Money(r.OriginalAmount),
            interest: Money(r.Interest),
            fine: Money(r.Fine),
            discount: Money(r.Discount),
            minAmount: Money(r.MinAmount),
            maxAmount: Money(r.MaxAmount),
            allowChangeValue: r.AllowChangeValue,
            dueDate: r.DueDate,
            isOverdue: r.IsOverdue,
            fee: Money(r.Fee),
            minimumScheduleDate: r.MinimumScheduleDate);

    private static PixRecord ToRecord(PixLookupSnapshot s)
        => new(
            ToRecord(s.Receiver),
            s.ReceiverIspb,
            s.ReceiverIspbName,
            s.ReceiverKind?.Id,
            s.Amount?.Amount,
            s.TotalAmount?.Amount,
            s.Interest?.Amount,
            s.Fine?.Amount,
            s.Discount?.Amount,
            s.ChangeAmount?.Amount,
            s.DueDate,
            s.ExpirationDate,
            s.CanBePaidWithDifferentValue,
            s.CanBePaid,
            s.CannotBePaidReason,
            s.IsDynamic,
            s.ConciliationIdentifier,
            s.Payer?.Name,
            s.Payer?.MaskedTaxId,
            s.Description,
            s.ConsultedAt);

    private static PixLookupSnapshot FromRecord(PixRecord r)
        => PixLookupSnapshot.Create(
            FromRecord(r.Receiver),
            r.ConsultedAt,
            canBePaid: r.CanBePaid,
            cannotBePaidReason: r.CannotBePaidReason,
            isDynamic: r.IsDynamic,
            receiverIspb: r.ReceiverIspb,
            receiverIspbName: r.ReceiverIspbName,
            receiverKind: r.ReceiverKind is null ? null : Enumeration.FromValue<TaxIdKind>(r.ReceiverKind.Value),
            amount: Money(r.Amount),
            totalAmount: Money(r.TotalAmount),
            interest: Money(r.Interest),
            fine: Money(r.Fine),
            discount: Money(r.Discount),
            changeAmount: Money(r.ChangeAmount),
            dueDate: r.DueDate,
            expirationDate: r.ExpirationDate,
            canBePaidWithDifferentValue: r.CanBePaidWithDifferentValue,
            conciliationIdentifier: r.ConciliationIdentifier,
            payer: r.PayerName is null && r.PayerMaskedTaxId is null
                ? null
                : MaskedParty.Of(r.PayerName, r.PayerMaskedTaxId),
            description: r.Description);

    private static PartyRecord ToRecord(LookupParty p) => new(p.Name, p.TradingName, p.TaxId?.Value);

    private static LookupParty FromRecord(PartyRecord r) => LookupParty.From(r.Name, r.TradingName, r.TaxId);

    private static Money? Money(decimal? amount)
        => amount is null ? null : new Money(amount.Value, Currency.BRL);

    private sealed record PartyRecord(string? Name, string? TradingName, string? TaxId);

    private sealed record BankSlipRecord(
        PartyRecord Beneficiary,
        string? BankCode,
        decimal? Amount,
        decimal? OriginalAmount,
        decimal? Interest,
        decimal? Fine,
        decimal? Discount,
        decimal? MinAmount,
        decimal? MaxAmount,
        bool AllowChangeValue,
        DateOnly? DueDate,
        bool IsOverdue,
        decimal? Fee,
        DateOnly? MinimumScheduleDate,
        DateTimeOffset ConsultedAt);

    private sealed record PixRecord(
        PartyRecord Receiver,
        string? ReceiverIspb,
        string? ReceiverIspbName,
        int? ReceiverKind,
        decimal? Amount,
        decimal? TotalAmount,
        decimal? Interest,
        decimal? Fine,
        decimal? Discount,
        decimal? ChangeAmount,
        DateOnly? DueDate,
        DateTimeOffset? ExpirationDate,
        bool CanBePaidWithDifferentValue,
        bool CanBePaid,
        string? CannotBePaidReason,
        bool IsDynamic,
        string? ConciliationIdentifier,
        string? PayerName,
        string? PayerMaskedTaxId,
        string? Description,
        DateTimeOffset ConsultedAt);

    private sealed record HistoryRecord(
        int Rail,
        int Status,
        BankSlipRecord? BankSlipSnapshot,
        PixRecord? PixSnapshot,
        string? ReasonCode,
        DateTimeOffset AttemptedAt);
}
