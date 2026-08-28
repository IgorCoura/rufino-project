namespace BillPayment.Application.Queries.Bills;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class BillQueries(BillPaymentDbContext context, UnlockedArtifactReader artifacts) : IBillQueries
{
    public const int DEFAULT_LIMIT = 50;
    public const int MAX_LIMIT = 200;

    public async Task<BillPage> ListAsync(
        Guid tenantId,
        string? status,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var effectiveLimit = Math.Clamp(limit <= 0 ? DEFAULT_LIMIT : limit, 1, MAX_LIMIT);
        var tenant = TenantId.From(tenantId);

        var query = context.Bills.AsNoTracking().Where(b => b.TenantId == tenant);

        if (TryParseStatus(status, out var parsed))
            query = query.Where(b => b.Status == parsed);

        // Keyset descendente por (CreatedAt, Id) — mais recente primeiro, que é a ordem em que a
        // fila é trabalhada. O desempate é DESCENDENTE junto com a chave: esta lista ordenava
        // CreatedAt desc e desempatava Id asc, e direções cruzadas fazem ORDER BY e WHERE
        // discordarem sobre quem já foi visto.
        if (CursorCodec.TryDecode(cursor, out var beforeCreatedAt, out var beforeId))
        {
            var beforeBillId = BillId.From(beforeId);

            query = query.Where(b =>
                b.CreatedAt < beforeCreatedAt || (b.CreatedAt == beforeCreatedAt && b.Id < beforeBillId));
        }

        var rows = await query
            .OrderByDescending(b => b.CreatedAt)
            .ThenByDescending(b => b.Id)
            .Take(effectiveLimit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > effectiveLimit;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        var nextCursor = hasMore && rows.Count > 0
            ? CursorCodec.Encode(rows[^1].CreatedAt, rows[^1].Id.Value)
            : null;

        return new BillPage(rows.ConvertAll(ToDto), nextCursor);
    }

    public async Task<BillDto?> GetAsync(
        Guid tenantId,
        Guid billId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var id = BillId.From(billId);

        var bill = await context.Bills
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.TenantId == tenant && b.Id == id, cancellationToken);

        return bill is null ? null : ToDto(bill);
    }

    public async Task<BillDetailDto?> GetDetailAsync(
        Guid tenantId,
        Guid billId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var id = BillId.From(billId);

        var bill = await context.Bills
            .AsNoTracking()
            .Include(b => b.Checks)
            .FirstOrDefaultAsync(b => b.TenantId == tenant && b.Id == id, cancellationToken);

        if (bill is null)
            return null;

        var beneficiary = bill.Beneficiary;

        var barcode = bill.Instruments.FirstOrDefault(i => i.Kind == PaymentInstrumentKind.Barcode);

        return new BillDetailDto(
            bill.Id.Value,
            bill.Status.Name,
            bill.Kind.Name,
            bill.Rail.Name,
            bill.Risk?.Name,
            beneficiary is null
                ? null
                : new BillPartyDto(beneficiary.Name, beneficiary.TradingName, beneficiary.TaxId?.Formatted()),
            bill.PayableAmount?.Amount,
            bill.Lookup?.OriginalAmount?.Amount,
            ToDateTime(bill.DueDate),
            bill.Lookup?.BankCode?.Value
                ?? (barcode is not null && barcode.DigitableLine.Kind.CarriesBankCode
                    ? barcode.DigitableLine.BankCode.Value
                    : null),
            ToDateTime(bill.Lookup?.MinimumScheduleDate),
            bill.LastConsultedAt?.UtcDateTime,

            ToReadingDto(bill.Reading),
            bill.ReadingState.Name,

            new BillLookupsDto(ToBankSlipLookupDto(bill.Lookup), ToPixLookupDto(bill.PixLookup)),

            // Ordem estável pelo id do tipo: a tela lista as doze sempre na mesma sequência,
            // e a do catálogo é a ordem de leitura que o doc 03 pede.
            [.. bill.Checks
                .OrderBy(c => c.Type.Id)
                .Select(c => new BillCheckDto(
                    c.Type.Name,
                    c.Outcome.Name,
                    c.Severity.Name,
                    c.ReasonCode,
                    c.Evidence,
                    c.IsBlockingFailure,
                    c.EvaluatedAt))],

            bill.Approval is null
                ? null
                : new BillApprovalDto(
                    bill.Approval.DecidedBy.Value,
                    bill.Approval.Decision.Name,
                    bill.Approval.DecidedAt,
                    bill.Approval.Note),
            ToDateTime(bill.ScheduledFor),
            new BillOriginDto(
                bill.Origin.SourceKind.Name,
                bill.Origin.SourceId,
                bill.Origin.SenderAddress,
                bill.Origin.ReceivedAt,
                !string.IsNullOrEmpty(bill.Origin.StorageKey)),
            bill.CreatedAt);
    }

    public async Task<ArtifactDownload?> GetArtifactAsync(
        Guid tenantId,
        Guid billId,
        CancellationToken cancellationToken = default)
    {
        var tenant = TenantId.From(tenantId);
        var id = BillId.From(billId);

        var bill = await context.Bills
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.TenantId == tenant && b.Id == id, cancellationToken);

        var storageKey = bill?.Origin.StorageKey;

        // Boleto importado à mão nasce só com os dígitos: não há arquivo, e isso é estado normal.
        if (string.IsNullOrEmpty(storageKey))
            return null;

        // A Bill não guarda tipo de mídia — o do balde é a única fonte, e o nome é montado a
        // partir do id porque nenhum nome de anexo sobrevive à promoção. O documento sai
        // destravado quando veio cifrado: o aprovador confere o papel, não a senha do emissor.
        return await artifacts.OpenAsync(
            tenant, storageKey, declaredContentType: null, $"boleto-{bill!.Id.Value:N}", cancellationToken);
    }

    private static bool TryParseStatus(string? status, out BillStatus parsed)
    {
        parsed = default!;
        if (string.IsNullOrWhiteSpace(status))
            return false;

        parsed = Enumeration.GetAll<BillStatus>()
            .FirstOrDefault(s => string.Equals(s.Name, status.Trim(), StringComparison.OrdinalIgnoreCase))!;

        return parsed is not null;
    }

    private static DateTime? ToDateTime(DateOnly? date)
        => date?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    private static BillPartyDto? ToPartyDto(LookupParty? party)
        => party is null
            ? null
            : new BillPartyDto(party.Name, party.TradingName, party.TaxId?.Formatted());

    // Os retratos saem POR INTEIRO (decisão de 2026-08-27): o aprovador vê exatamente o que o
    // provedor devolveu, e decide com a mesma informação que o sistema usou para classificar.
    private static BankSlipLookupDto? ToBankSlipLookupDto(LookupSnapshot? snapshot)
        => snapshot is null
            ? null
            : new BankSlipLookupDto(
                ToPartyDto(snapshot.Beneficiary),
                snapshot.BankCode?.Value,
                snapshot.Amount?.Amount,
                snapshot.OriginalAmount?.Amount,
                snapshot.Fee?.Amount,
                snapshot.AllowChangeValue,
                snapshot.IsOverdue,
                ToDateTime(snapshot.DueDate),
                ToDateTime(snapshot.MinimumScheduleDate),
                snapshot.ConsultedAt.UtcDateTime);

    private static PixLookupDto? ToPixLookupDto(PixLookupSnapshot? snapshot)
        => snapshot is null
            ? null
            : new PixLookupDto(
                ToPartyDto(snapshot.Receiver),
                snapshot.ReceiverIspb,
                snapshot.ReceiverIspbName,
                snapshot.IsDynamic,
                snapshot.CanBePaid,
                snapshot.Amount?.Amount,
                snapshot.TotalAmount?.Amount,
                snapshot.Interest?.Amount,
                snapshot.Fine?.Amount,
                snapshot.Discount?.Amount,
                ToDateTime(snapshot.DueDate),
                snapshot.ExpirationDate?.UtcDateTime,
                snapshot.ConsultedAt.UtcDateTime);

    private static BillReadingDto? ToReadingDto(DocumentReading? reading)
        => reading is null
            ? null
            : new BillReadingDto(
                reading.PayerName,
                reading.PayerTaxId?.Formatted(),
                reading.PayeeName,
                reading.PayeeTaxId?.Formatted(),
                reading.AccountReference,
                reading.Amount,
                ToDateTime(reading.DueDate),
                reading.BillingPeriodText,
                reading.Competence?.Year,
                reading.Competence?.Month,
                reading.Description,
                reading.ReadAt.UtcDateTime);

    /// <summary>
    /// O beneficiário que a tela mostra: o oficial quando a consulta resolveu; senão, o que a
    /// leitura por IA viu.
    /// </summary>
    /// <remarks>
    /// A precedência é a mesma do detalhe, e a ordem importa: o retrato oficial é constatado, a
    /// leitura é candidata. Inverter faria a tela afirmar como verificado um nome que só foi lido.
    /// O da leitura sai <strong>sem nome fantasia</strong>, que é o que o rotula como não-oficial.
    /// </remarks>
    private static BillPartyDto? BeneficiaryOf(Bill bill)
    {
        if (bill.Beneficiary is { } official)
            return new BillPartyDto(official.Name, official.TradingName, official.TaxId?.Formatted());

        if (bill.Reading is not { } reading)
            return null;

        return reading.PayeeName is null && reading.PayeeTaxId is null
            ? null
            : new BillPartyDto(reading.PayeeName, null, reading.PayeeTaxId?.Formatted());
    }

    private static BillDto ToDto(Bill bill)
    {
        var barcode = bill.Instruments.FirstOrDefault(i => i.Kind == PaymentInstrumentKind.Barcode);

        // Projeção read-only para montar a resposta — não decide nada de domínio. Vencimento,
        // valor e beneficiário vêm do agregado, que consolida consulta oficial e instrumento
        // com a mesma precedência do detalhe; o valor declarado no instrumento é a reserva
        // para boleto ainda não consultado.
        var declared = bill.Instruments
            .Select(i => i.DeclaredAmount)
            .FirstOrDefault(a => a is not null);

        // Boleto ainda sem consulta resolvida mostra o beneficiário que a leitura por IA viu —
        // rotulado pela ausência de documento oficial, nunca confundível com o verificado.
        var beneficiary = BeneficiaryOf(bill);

        return new BillDto(
            bill.Id.Value,
            bill.Status.Name,
            bill.Kind.Name,
            bill.Rail.Name,
            bill.Risk?.Name,
            beneficiary,
            bill.PayableAmount?.Amount ?? declared?.Amount,
            ToDateTime(bill.DueDate),
            barcode is not null && barcode.DigitableLine.Kind.CarriesBankCode
                ? barcode.DigitableLine.BankCode.Value
                : null,
            new BillOriginDto(
                bill.Origin.SourceKind.Name,
                bill.Origin.SourceId,
                bill.Origin.SenderAddress,
                bill.Origin.ReceivedAt,
                !string.IsNullOrEmpty(bill.Origin.StorageKey)),
            bill.CreatedAt,
            bill.ReadingState.Name);
    }
}
