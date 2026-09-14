namespace BillPayment.Application.Queries.Bills;

using BillPayment.Domain.SeedWork;

/// <summary>Quanto do documento de cada boleto entra na exportação.</summary>
/// <remarks>
/// Vale só para o documento do boleto: o comprovante sai sempre inteiro — decisão do usuário em
/// 2026-09-14.
/// </remarks>
public sealed class BillDocumentPages : Enumeration
{
    public static readonly BillDocumentPages All = new(1, "All");
    public static readonly BillDocumentPages FirstPage = new(2, "FirstPage");

    private BillDocumentPages(int id, string name) : base(id, name) { }

    /// <summary>O teto de páginas que a composição recebe. Nulo = todas.</summary>
    public int? MaxPages => this == FirstPage ? 1 : null;
}

/// <summary>Como os documentos saem: um PDF só, ou um PDF por boleto dentro de um .zip.</summary>
public sealed class BillDocumentPackaging : Enumeration
{
    public static readonly BillDocumentPackaging SinglePdf = new(1, "SinglePdf");
    public static readonly BillDocumentPackaging PdfPerBill = new(2, "PdfPerBill");

    private BillDocumentPackaging(int id, string name) : base(id, name) { }
}

/// <summary>O pedido de exportação, já traduzido da borda HTTP.</summary>
/// <param name="BillIds">
/// Os boletos <strong>na ordem em que foram marcados</strong> — é a ordem do arquivo.
/// </param>
public sealed record BillDocumentExportRequest(
    IReadOnlyList<Guid> BillIds,
    BillDocumentPages Pages,
    bool IncludeReceipts,
    BillDocumentPackaging Packaging);

/// <summary>O arquivo pronto para servir, e o que entrou de cada boleto — para a trilha.</summary>
public sealed record BillDocumentExportFile(
    byte[] Content,
    string ContentType,
    string FileName,
    IReadOnlyList<ExportedBillDocument> Bills);

/// <param name="Unlocked">O documento saiu como cópia sem senha de um original cifrado.</param>
/// <param name="PaymentCodeDisclosed">
/// A página de aviso escreveu a linha digitável ou o Pix — a trilha precisa distinguir isso de
/// servir o documento.
/// </param>
public sealed record ExportedBillDocument(Guid BillId, bool Unlocked, bool PaymentCodeDisclosed);
