namespace BillPayment.Application.PaymentOrders;

/// <summary>
/// O comprovante que foi guardado como a PÁGINA do provedor, e não como o arquivo.
/// </summary>
/// <remarks>
/// <para>
/// Até 2026-09-09 o adapter baixava <c>transactionReceiptUrl</c> e gravava o que viesse — e o que
/// vem é <c>text/html</c>, a página do comprovante. A ordem ficava com chave, <c>hasReceipt</c>
/// verdadeiro e a tela do usuário vazia, porque o app só renderiza PDF e imagem. Essas chaves
/// existem no balde e precisam ser refeitas.
/// </para>
/// <para>
/// <strong>A regra mora aqui porque tem dois consumidores, e os dois são da Application</strong>:
/// a guarda de idempotência do <c>CapturePaymentReceiptCommandHandler</c> e a cláusula da
/// varredura em <c>PaymentOrderWorkQueries</c>. Um deles é SQL cru e não alcança o C#, então o
/// que se compartilha é a <see cref="EXTENSION"/> — escrever <c>'%.html'</c> à mão na consulta
/// faria a regra existir em dois lugares, e um deles envelheceria.
/// </para>
/// <para>
/// Não é invariante do agregado: <c>PaymentOrder</c> só exige que a chave exista e que a ordem
/// esteja paga (<c>BLP.PMO14</c>/<c>BLP.PMO15</c>). "Aquela chave aponta para a página em vez do
/// arquivo" é conserto de captura, e some quando o acervo terminar de ser repescado.
/// </para>
/// </remarks>
internal static class LandingPageReceipt
{
    /// <summary>A extensão que o nome do arquivo recebe quando o provedor devolve HTML.</summary>
    public const string EXTENSION = ".html";

    /// <summary>O mesmo critério em forma de <c>LIKE</c>, para a varredura.</summary>
    public const string KEY_LIKE_PATTERN = "%" + EXTENSION;

    /// <summary>
    /// A chave guardada é a página, e não o comprovante? Chave ausente devolve <c>false</c> —
    /// "não há nada guardado" é outra pergunta, e quem a faz confere a chave vazia.
    /// </summary>
    public static bool WasStoredAt(string? storageKey)
        => !string.IsNullOrEmpty(storageKey)
            && storageKey.EndsWith(EXTENSION, StringComparison.OrdinalIgnoreCase);
}
