namespace BillPayment.Domain.Instruments;

using BillPayment.Domain.SeedWork;

/// <summary>
/// A natureza do documento, deduzida do próprio código de barras. Não é rótulo escolhido
/// por quem cadastra — é o que a linha digitável diz que ele é.
/// </summary>
/// <remarks>
/// A distinção não é cosmética: os dois tipos têm layout, quantidade de dígitos, regra de
/// dígito verificador e <strong>campos disponíveis</strong> diferentes. Arrecadação não tem
/// banco emissor em posição nenhuma, e é isso que torna o check de banco inaplicável a ela
/// (ver `03-bill-validation.md`, check 6).
/// </remarks>
public sealed class BillKind : Enumeration
{
    /// <summary>Ficha de compensação: 47 dígitos na linha digitável, 44 no código de barras.</summary>
    public static readonly BillKind BankSlip = new(
        id: 1,
        name: "BankSlip",
        digitableLineLength: 47,
        carriesBankCode: true);

    /// <summary>Conta de convênio (concessionária, tributo): 48 dígitos na linha digitável, 44 no código de barras.</summary>
    public static readonly BillKind Utility = new(
        id: 2,
        name: "Utility",
        digitableLineLength: 48,
        carriesBankCode: false);

    public int DigitableLineLength { get; }

    /// <summary>
    /// Se o código de barras carrega o COMPE do banco liquidante nas posições 1–3.
    /// Falso em arrecadação: lá as três primeiras posições são produto, segmento e
    /// identificador de valor, e contas de convênio liquidam fora da compensação.
    /// </summary>
    public bool CarriesBankCode { get; }

    private BillKind(int id, string name, int digitableLineLength, bool carriesBankCode)
        : base(id, name)
    {
        DigitableLineLength = digitableLineLength;
        CarriesBankCode = carriesBankCode;
    }
}
