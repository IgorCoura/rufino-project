namespace BillPayment.Infra.Extraction;

using Ganss.Xss;

/// <summary>
/// O HTML do e-mail capturado, pronto para uma tela renderizar: sem script, sem iframe, sem
/// <c>on*</c>, sem <c>javascript:</c>.
/// </summary>
/// <remarks>
/// <para>
/// O corpo vem de um remetente da internet e é servido ao tenant como HTML. Até 2026-08-28 ia
/// cru — o cliente Flutter não executa script, mas a API não pode depender do cliente para não
/// entregar conteúdo ativo. Sanitizar no servidor é o único lugar que cobre todo consumidor.
/// </para>
/// <para>
/// Só HTML passa por aqui; texto puro sai como está. A extração de instrumentos lê o corpo por
/// outro caminho (<see cref="HtmlText"/>) e não é afetada — sanitizar antes de extrair poderia
/// engolir um BR Code dentro de um atributo que o sanitizador descarta.
/// </para>
/// </remarks>
public static class EmailBodySanitizer
{
    public static string Sanitize(string html)
    {
        if (string.IsNullOrEmpty(html))
            return html;

        // Uma instância por chamada: a biblioteca não garante thread-safety, e o custo é
        // desprezível diante do download que antecede.
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedSchemes.Remove("javascript");

        return sanitizer.Sanitize(html);
    }
}
