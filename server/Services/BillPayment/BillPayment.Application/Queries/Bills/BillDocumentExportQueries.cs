namespace BillPayment.Application.Queries.Bills;

using System.Globalization;
using System.IO.Compression;
using System.Text;
using BillPayment.Application.PaymentOrders;
using BillPayment.Application.PaymentOrders.Commands;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

/// <summary>
/// Baixa os documentos de vários boletos de uma vez: um PDF só, ou um PDF por boleto num .zip.
/// </summary>
public interface IBillDocumentExportQueries
{
    /// <summary>
    /// Monta o arquivo. <c>null</c> quando algum boleto pedido não existe neste tenant — a
    /// resposta é a mesma de um id inexistente, e não revela nada.
    /// </summary>
    Task<BillDocumentExportFile?> ExportAsync(
        Guid tenantId,
        BillDocumentExportRequest request,
        CancellationToken cancellationToken = default);
}

/// <remarks>
/// <para>
/// <strong>Todo boleto selecionado deixa rastro no arquivo.</strong> Sem documento guardado, ou
/// com um documento que não abre, entra uma página de aviso no lugar — com a procedência do
/// código e o próprio código de pagamento. Um boleto sumir em silêncio de dentro do PDF faria a
/// pessoa acreditar que baixou tudo.
/// </para>
/// <para>
/// <strong>A página de aviso escreve a linha digitável e o Pix, e isso é exceção deliberada</strong>
/// à regra de que os dígitos não saem pela API (decisão do usuário em 2026-09-14). O portão é o
/// mesmo do documento original — <c>bill:view</c> —, e o documento original já traz o código
/// impresso; o que o aviso faz é não deixar o boleto sem documento pior servido que o outro.
/// </para>
/// </remarks>
internal sealed class BillDocumentExportQueries(
    BillPaymentDbContext context,
    UnlockedArtifactReader artifacts,
    IAttachmentStorage storage,
    IPdfComposer composer,
    TimeProvider clock,
    IOptions<PaymentSchedulingOptions> scheduling) : IBillDocumentExportQueries
{
    public const int MAX_BILLS = 50;

    /// <summary>Soma dos documentos lidos. O arquivo é montado em memória, numa requisição.</summary>
    public const int MAX_TOTAL_MEGABYTES = 200;

    private const string PDF = "application/pdf";
    private const string ZIP = "application/zip";

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<BillDocumentExportFile?> ExportAsync(
        Guid tenantId,
        BillDocumentExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ordered = request.BillIds.Distinct().ToList();
        if (ordered.Count == 0)
            throw BillErrors.DocumentExportSelectionEmpty();
        if (ordered.Count > MAX_BILLS)
            throw BillErrors.DocumentExportSelectionTooLarge(MAX_BILLS);

        var tenant = TenantId.From(tenantId);
        var ids = ordered.ConvertAll(BillId.From);

        var bills = await context.Bills
            .AsNoTracking()
            .Where(b => b.TenantId == tenant && ids.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, cancellationToken);

        if (bills.Count != ids.Count)
            return null;

        var orders = request.IncludeReceipts
            ? (await context.PaymentOrders
                .AsNoTracking()
                .Where(o => o.TenantId == tenant && ids.Contains(o.BillId))
                .ToListAsync(cancellationToken))
                .ToLookup(o => o.BillId)
            : null;

        var zone = scheduling.Value.ResolveTimeZone();
        var today = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var services = new ExportServices(context, artifacts, storage, composer, zone, today);
        var export = new Export(services, tenant, request, orders);

        return request.Packaging == BillDocumentPackaging.SinglePdf
            ? await export.SinglePdfAsync(ids.ConvertAll(id => bills[id]), cancellationToken)
            : await export.PdfPerBillAsync(ids.ConvertAll(id => bills[id]), cancellationToken);
    }

    /// <summary>O que a exportação consulta, e o "hoje" resolvido uma vez no fuso da política.</summary>
    private sealed record ExportServices(
        BillPaymentDbContext Context,
        UnlockedArtifactReader Artifacts,
        IAttachmentStorage Storage,
        IPdfComposer Composer,
        TimeZoneInfo Zone,
        string Today);

    /// <summary>Uma exportação em curso: o orçamento de bytes e o registro do que entrou.</summary>
    private sealed class Export(
        ExportServices services,
        TenantId tenant,
        BillDocumentExportRequest request,
        ILookup<BillId, PaymentOrder>? ordersByBill)
    {
        private readonly List<ExportedBillDocument> _exported = [];
        private long _bytesRead;

        public async Task<BillDocumentExportFile> SinglePdfAsync(
            List<Bill> bills,
            CancellationToken cancellationToken)
        {
            using var composition = services.Composer.Begin();

            foreach (var bill in bills)
                await AppendBillAsync(composition, bill, cancellationToken);

            return new BillDocumentExportFile(
                composition.Build(), PDF, $"boletos-{services.Today}.pdf", _exported);
        }

        public async Task<BillDocumentExportFile> PdfPerBillAsync(
            List<Bill> bills,
            CancellationToken cancellationToken)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var files = new List<(string Name, byte[] Content)>(bills.Count);

            foreach (var bill in bills)
            {
                using var composition = services.Composer.Begin();
                await AppendBillAsync(composition, bill, cancellationToken);
                files.Add((UniqueName(names, FileNameFor(bill)), composition.Build()));
            }

            // Um boleto só não precisa de envelope: o .zip seria um clique a mais por nada.
            if (files.Count == 1)
                return new BillDocumentExportFile(files[0].Content, PDF, files[0].Name, _exported);

            using var buffer = new MemoryStream();
            using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var (name, content) in files)
                {
                    // PDF já é comprimido; comprimir de novo gasta CPU sem ganho.
                    var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
                    await using var stream = await entry.OpenAsync(cancellationToken);
                    await stream.WriteAsync(content, cancellationToken);
                }
            }

            return new BillDocumentExportFile(
                buffer.ToArray(), ZIP, $"boletos-{services.Today}.zip", _exported);
        }

        /// <summary>Dois boletos do mesmo beneficiário e vencimento não podem sobrescrever um ao outro no .zip.</summary>
        private static string UniqueName(HashSet<string> taken, string name)
        {
            var stem = name[..^4];
            var candidate = name;
            var suffix = 1;

            while (!taken.Add(candidate))
            {
                suffix++;
                candidate = $"{stem} ({suffix}).pdf";
            }

            return candidate;
        }

        private async Task AppendBillAsync(IPdfComposition composition, Bill bill, CancellationToken cancellationToken)
        {
            var (appended, unlocked) = await TryAppendDocumentAsync(composition, bill, cancellationToken);

            if (!appended)
            {
                var hadStoredDocument = !string.IsNullOrEmpty(bill.Origin.StorageKey);
                composition.AppendNotice(await MissingDocumentNoticeAsync(bill, hadStoredDocument, cancellationToken));
            }

            _exported.Add(new ExportedBillDocument(bill.Id.Value, unlocked, PaymentCodeDisclosed: !appended));

            if (request.IncludeReceipts)
                await AppendReceiptAsync(composition, bill, cancellationToken);
        }

        private async Task<(bool Appended, bool Unlocked)> TryAppendDocumentAsync(
            IPdfComposition composition,
            Bill bill,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(bill.Origin.StorageKey))
                return (false, false);

            using var download = await services.Artifacts.OpenAsync(
                tenant, bill.Origin.StorageKey, declaredContentType: null, $"boleto-{bill.Id.Value:N}", cancellationToken);

            if (download is null)
                return (false, false);

            var bytes = await ReadAsync(download.Content, cancellationToken);

            return composition.TryAppend(bytes, download.ContentType, request.Pages.MaxPages)
                ? (true, download.Unlocked)
                : (false, false);
        }

        private async Task AppendReceiptAsync(IPdfComposition composition, Bill bill, CancellationToken cancellationToken)
        {
            var orders = ordersByBill![bill.Id].ToList();

            var paid = orders.Any(o => o.Status == PaymentOrderStatus.Paid || o.Status == PaymentOrderStatus.Refunded);
            var withReceipt = orders
                .Where(o => !string.IsNullOrEmpty(o.ReceiptStorageKey))
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefault();

            // Boleto que nunca foi pago não tem comprovante, e isso não merece aviso.
            if (!paid && withReceipt is null)
                return;

            if (withReceipt is not null
                && !LandingPageReceipt.WasStoredAt(withReceipt.ReceiptStorageKey)
                && await TryAppendReceiptAsync(composition, withReceipt, cancellationToken))
            {
                return;
            }

            composition.AppendNotice(MissingReceiptNotice(bill, withReceipt ?? orders.FirstOrDefault(IsPaid)));
        }

        private async Task<bool> TryAppendReceiptAsync(
            IPdfComposition composition,
            PaymentOrder order,
            CancellationToken cancellationToken)
        {
            var stored = await services.Storage.OpenAsync(tenant, order.ReceiptStorageKey!, cancellationToken);
            if (stored is null)
                return false;

            using var download = await Queries.ArtifactDownload.OpenAsync(
                stored, null, $"comprovante-{order.Id.Value}", cancellationToken);

            var bytes = await ReadAsync(download.Content, cancellationToken);
            return composition.TryAppend(bytes, download.ContentType);
        }

        private async Task<byte[]> ReadAsync(Stream content, CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);

            _bytesRead += buffer.Length;
            if (_bytesRead > MAX_TOTAL_MEGABYTES * 1024L * 1024L)
                throw BillErrors.DocumentExportTooHeavy(MAX_TOTAL_MEGABYTES);

            return buffer.ToArray();
        }

        private async Task<PdfNotice> MissingDocumentNoticeAsync(
            Bill bill,
            bool hadStoredDocument,
            CancellationToken cancellationToken)
        {
            var message = hadStoredDocument
                ? "O documento guardado deste boleto não pôde ser aberto — protegido por uma senha que o sistema "
                    + "não conhece, ou corrompido. Abaixo, a procedência e o código para pagamento que o sistema registrou."
                : "Este boleto não tem documento guardado. Abaixo, a procedência e o código para pagamento que o "
                    + "sistema registrou.";

            var fields = new List<PdfNoticeField>(BillSummary(bill));
            fields.AddRange(await OriginFieldsAsync(bill, cancellationToken));

            foreach (var instrument in bill.Instruments)
            {
                fields.Add(instrument.Kind == PaymentInstrumentKind.Barcode
                    ? new PdfNoticeField("Linha digitável", FormatDigitableLine(instrument.DigitableLine.Value))
                    : new PdfNoticeField("Pix copia e cola", instrument.PixPayload.Payload));
            }

            return new PdfNotice(
                hadStoredDocument ? "Documento do boleto indisponível" : "Boleto sem documento",
                message,
                fields);
        }

        private static PdfNotice MissingReceiptNotice(Bill bill, PaymentOrder? order)
        {
            var fields = new List<PdfNoticeField>(BillSummary(bill));
            if (order?.PaidAt is { } paidAt)
                fields.Add(new PdfNoticeField("Pago em", paidAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));

            return new PdfNotice(
                "Comprovante indisponível",
                "Este boleto consta como pago, mas o comprovante de pagamento ainda não foi obtido do provedor.",
                fields);
        }

        private static IEnumerable<PdfNoticeField> BillSummary(Bill bill)
        {
            var beneficiary = BillQueries.BeneficiaryOf(bill);
            var name = beneficiary?.Name ?? beneficiary?.TradingName;
            var amount = bill.PayableAmount?.Amount
                ?? bill.Instruments.Select(i => i.DeclaredAmount).FirstOrDefault(a => a is not null)?.Amount;

            yield return new PdfNoticeField(
                "Beneficiário",
                string.Join(" · ", new[] { name, beneficiary?.TaxId }.Where(v => !string.IsNullOrWhiteSpace(v)))
                    is { Length: > 0 } text ? text : "Não identificado");

            yield return new PdfNoticeField(
                "Valor",
                amount is null ? "Não informado" : "R$ " + amount.Value.ToString("N2", PtBr));

            yield return new PdfNoticeField(
                "Vencimento",
                bill.DueDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "Não informado");
        }

        private async Task<IEnumerable<PdfNoticeField>> OriginFieldsAsync(Bill bill, CancellationToken cancellationToken)
        {
            var origin = bill.Origin;
            var receivedAt = FormatInstant(origin.ReceivedAt);

            if (origin.SourceKind == BillSourceKind.ManualUpload)
            {
                return
                [
                    new PdfNoticeField("Origem do código", "Inserido manualmente"),
                    new PdfNoticeField("Importado em", receivedAt),
                ];
            }

            if (origin.SourceKind == BillSourceKind.Portal)
            {
                return
                [
                    new PdfNoticeField("Origem do código", "Portal do emissor"),
                    new PdfNoticeField("Obtido em", receivedAt),
                ];
            }

            var subject = await SubjectOfAsync(bill, cancellationToken);
            var fields = new List<PdfNoticeField> { new("Origem do código", "E-mail") };

            if (!string.IsNullOrWhiteSpace(origin.SenderAddress))
                fields.Add(new PdfNoticeField("Remetente", origin.SenderAddress));
            if (!string.IsNullOrWhiteSpace(subject))
                fields.Add(new PdfNoticeField("Assunto", subject));

            fields.Add(new PdfNoticeField("Recebido em", receivedAt));
            return fields;
        }

        private async Task<string?> SubjectOfAsync(Bill bill, CancellationToken cancellationToken)
        {
            if (bill.Origin.SourceId is not { } sourceId || string.IsNullOrEmpty(bill.Origin.ExternalMessageId))
                return null;

            var source = CaptureSourceId.From(sourceId);
            var externalMessageId = bill.Origin.ExternalMessageId;

            return await services.Context.CapturedMessages
                .AsNoTracking()
                .Where(m => m.TenantId == tenant && m.SourceId == source && m.ExternalMessageId == externalMessageId)
                .Select(m => m.Subject)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private string FormatInstant(DateTime utc)
        {
            var asUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(asUtc, services.Zone).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        }

        private static bool IsPaid(PaymentOrder order)
            => order.Status == PaymentOrderStatus.Paid || order.Status == PaymentOrderStatus.Refunded;
    }

    /// <summary>
    /// A linha digitável como ela é impressa: cobrança em cinco campos, arrecadação em quatro
    /// blocos com o dígito de cada um.
    /// </summary>
    internal static string FormatDigitableLine(string digits)
    {
        if (digits.Length == 47)
        {
            return $"{digits[..5]}.{digits[5..10]} {digits[10..15]}.{digits[15..21]} "
                + $"{digits[21..26]}.{digits[26..32]} {digits[32]} {digits[33..]}";
        }

        if (digits.Length == 48)
        {
            var blocks = Enumerable.Range(0, 4).Select(i => $"{digits.Substring(i * 12, 11)}-{digits[(i * 12) + 11]}");
            return string.Join(' ', blocks);
        }

        return digits;
    }

    /// <summary>
    /// <c>boleto-BENEFICIARIO-2026-09-25.pdf</c>, no mesmo formato do nome que o app sugere no
    /// download avulso.
    /// </summary>
    internal static string FileNameFor(Bill bill)
    {
        var beneficiary = BillQueries.BeneficiaryOf(bill);
        var parts = new List<string> { "boleto" };

        var slug = Slug(beneficiary?.Name ?? beneficiary?.TradingName);
        if (slug.Length > 0)
            parts.Add(slug);

        if (bill.DueDate is { } due)
            parts.Add(due.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        if (parts.Count == 1)
            parts.Add(bill.Id.Value.ToString("N", CultureInfo.InvariantCulture)[..8]);

        return string.Join('-', parts) + ".pdf";
    }

    /// <summary>Sem acento, só letra, dígito e hífen, no máximo 60 — sobrevive a qualquer sistema de arquivos.</summary>
    private static string Slug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            builder.Append(char.IsAsciiLetterOrDigit(c) ? c : '-');
        }

        var slug = string.Join('-', builder.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        return slug.Length > 60 ? slug[..60].TrimEnd('-') : slug;
    }
}
