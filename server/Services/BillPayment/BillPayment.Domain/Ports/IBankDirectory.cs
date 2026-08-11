namespace BillPayment.Domain.Ports;

using BillPayment.Domain.SharedKernel;

/// <summary>
/// A tabela de instituições do Banco Central, na forma que o domínio precisa dela: responder
/// se um código de banco existe, se ele liquida boleto, e traduzir entre os dois sistemas de
/// identificação que o país usa.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Por que isto é uma porta e não um dado do VO.</strong> <c>DigitableLine</c> valida a
/// <em>estrutura</em> do código de barras — DVs, layout, banco não atribuído. Saber se "479"
/// corresponde a uma instituição real é conhecimento de mundo externo, que muda sem que o
/// código mude. Enfiar essa tabela dentro do VO o tornaria impuro e o obrigaria a mudar
/// sempre que o Bacen publicasse uma instituição nova.
/// </para>
/// <para>
/// <strong>Sem <c>CancellationToken</c> e sem <c>Task</c> de propósito.</strong> A implementação
/// lê um snapshot em memória, não faz I/O. Um check de pagamento não pode depender de um site
/// externo estar no ar: indisponibilidade do Bacen viraria bloqueio de pagamento.
/// </para>
/// </remarks>
public interface IBankDirectory
{
    /// <summary>O código corresponde a alguma instituição registrada no Bacen?</summary>
    bool IsKnown(BankCode bankCode);

    /// <summary>
    /// A instituição participa da Compe? Boleto liquida por ali, então um código válido que
    /// <em>não</em> participa é anomalia — existe, mas não deveria estar num código de barras.
    /// </summary>
    bool ParticipatesInCompe(BankCode bankCode);

    /// <summary>Nome reduzido da instituição, para a evidência do check. Nulo se desconhecida.</summary>
    string? NameOf(BankCode bankCode);

    /// <summary>
    /// Traduz o ISPB de 8 dígitos (identificação do trilho Pix) para o código de três dígitos
    /// (identificação do trilho boleto). É o que permite comparar o recebedor de um QR Pix
    /// contra os bancos aceitos de um <c>Payee</c>, que estão em COMPE.
    /// </summary>
    /// <returns>Nulo quando o ISPB é desconhecido ou quando a instituição não tem código de três dígitos.</returns>
    BankCode? FromIspb(string ispb);
}
