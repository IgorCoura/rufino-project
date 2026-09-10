namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;

/// <summary>
/// De onde saiu um campo de identidade da leitura por IA.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque a leitura funde duas fontes de confiabilidade muito diferentes.</strong>
/// O extrator recebe o documento <em>e</em> o corpo do e-mail (<c>SupplementalText</c>), e o corpo
/// é escrito por quem manda a mensagem. Um documento fiscal que só aparece ali pode ter sido
/// plantado — para <em>bloquear</em> um boleto legítimo (negação de serviço contra os pagamentos
/// de quem recebe) ou para <em>silenciar</em> a verificação, casando de propósito com o oficial.
/// </para>
/// <para>
/// <strong>A apuração é determinística, nunca declarada pelo modelo.</strong> Perguntar ao
/// extrator de onde ele leu seria confiar no mesmo canal que a injeção controla; o que o domínio
/// faz é procurar os dígitos no texto do e-mail — e isso a instrução injetada não muda.
/// </para>
/// </remarks>
public sealed class ReadingFieldSource : Enumeration
{
    /// <summary>Não veio do corpo do e-mail: ou está no documento, ou o modelo o compôs.</summary>
    public static readonly ReadingFieldSource Document = new(1, nameof(Document));

    /// <summary>
    /// Os dígitos aparecem no corpo do e-mail. Não perde o direito de virar aviso — perde o de
    /// bloquear sozinho.
    /// </summary>
    public static readonly ReadingFieldSource EmailBody = new(2, nameof(EmailBody));

    /// <summary>O campo não foi lido. Ausência não tem procedência.</summary>
    public static readonly ReadingFieldSource NotRead = new(3, nameof(NotRead));

    private ReadingFieldSource(int id, string name) : base(id, name) { }
}
