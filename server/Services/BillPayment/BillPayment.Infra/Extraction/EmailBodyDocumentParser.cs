namespace BillPayment.Infra.Extraction;

using System.Text;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.Ports;

/// <summary>
/// O degrau mais barato da cascata: o instrumento escrito no próprio corpo da mensagem.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Nasceu de uma medição que contrariou o plano da 2.5.</strong> A sprint estava desenhada
/// como "escada de resolução de link", e a caixa real mostrou que em dois dos cinco arquétipos o
/// dado pagável já estava no texto do e-mail: a SABESP manda o BR Code inteiro
/// (<c>00020101021226770014BR.GOV.BCB.PIX…</c>) no formato novo, e a linha digitável de arrecadação
/// no formato antigo. Os dois resolvem <strong>sem abrir arquivo e sem tocar a rede</strong>.
/// </para>
/// <para>
/// <strong>Por isso ele roda antes de qualquer link.</strong> Buscar o PDF de uma fatura cujo Pix
/// já está no corpo seria gastar uma chamada de rede — e abrir superfície de ataque — para
/// descobrir o que estava escrito ali.
/// </para>
/// <para>
/// Senha derivada não se aplica: corpo de e-mail não é cifrado. O parâmetro existe porque a porta
/// é uma só para toda a cascata.
/// </para>
/// </remarks>
internal sealed class EmailBodyDocumentParser : IBoletoDocumentParser
{
    /// <summary>
    /// Corpo de e-mail não é cifrado: não há cópia a produzir.
    /// </summary>
    public Task<ReadOnlyMemory<byte>?> UnlockAsync(
        ReadOnlyMemory<byte> content,
        string? contentType,
        IReadOnlyList<PasswordCandidate> passwordCandidates,
        CancellationToken cancellationToken)
        => Task.FromResult<ReadOnlyMemory<byte>?>(null);

    public Task<ExtractionResult> ParseAsync(
        ReadOnlyMemory<byte> content,
        string? contentType,
        IReadOnlyList<PasswordCandidate> passwordCandidates,
        IReadOnlyList<TaxId> knownTaxIds,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var text = ToText(content.Span);

        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(ExtractionResult.NotFound("empty_body"));

        var instruments = CandidateScanner.Scan(text, today.ToDateTime(TimeOnly.MinValue));

        // "Não há instrumento no corpo" não é o fim da linha: é exatamente o caso em que a escada
        // de link entra, e o motivo precisa dizer isso para a métrica separar um do outro.
        return Task.FromResult(instruments.Count == 0
            ? ExtractionResult.NotFound("no_instrument_in_body")
            : ExtractionResult.Found(
                instruments, ExtractionMethod.EmailBody, unlockedBy: null, TaxIdScanner.Scan(text, knownTaxIds)));
    }

    /// <summary>
    /// Converte os bytes do corpo em texto, tirando a marcação quando há.
    /// </summary>
    /// <remarks>
    /// O BR Code não pode passar por <c>HtmlDecode</c> antes de a marcação sair, senão uma entidade
    /// no meio do payload (que o emissor escapou para caber no HTML) o deixaria com caracteres a
    /// mais e o CRC não fecharia.
    /// </remarks>
    private static string ToText(ReadOnlySpan<byte> content)
    {
        var raw = Encoding.UTF8.GetString(content);

        return HtmlText.LooksLikeHtml(content) ? HtmlText.ToPlainText(raw) : raw;
    }
}
