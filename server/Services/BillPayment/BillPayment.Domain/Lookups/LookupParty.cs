namespace BillPayment.Domain.Lookups;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Quem a fonte oficial diz que é o beneficiário. Mesmo formato nos dois trilhos: o boleto
/// devolve <c>beneficiaryName</c>/<c>companyName</c>/<c>beneficiaryCpfCnpj</c>; o Pix devolve
/// <c>name</c>/<c>tradingName</c>/<c>cpfCnpj</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O documento é opcional porque a medição disse que ele falta.</strong> Nenhuma das dez
/// linhas de arrecadação consultadas na sprint 1.0 trouxe <c>beneficiaryCpfCnpj</c>, e todas
/// trouxeram nome. Declarar <see cref="TaxId"/> como obrigatório modelaria um mundo que não
/// existe e obrigaria o adapter a inventar dado — o check é que degrada para nome
/// (ver <c>03-bill-validation.md</c>, verificação 5).
/// </para>
/// <para>
/// O cotejo com o cadastro é de <c>Payee.MatchesName</c>, que compara sem distinção de caixa.
/// Este VO só apara espaços: fazer normalização própria aqui criaria uma segunda noção de
/// "nome igual" divergente da do beneficiário cadastrado.
/// </para>
/// </remarks>
public sealed class LookupParty : ValueObject
{
    public const int NAME_MAX_LENGTH = 200;

    /// <summary>Razão social ou nome, como o provedor devolveu.</summary>
    public string? Name { get; private set; }

    /// <summary>Nome fantasia, quando o provedor separa os dois.</summary>
    public string? TradingName { get; private set; }

    /// <summary>CPF ou CNPJ. Nulo em arrecadação e em parte das respostas de Pix.</summary>
    public TaxId? TaxId { get; private set; }

    private LookupParty() { }

    public static LookupParty Of(string? name, string? tradingName = null, TaxId? taxId = null)
    {
        var trimmedName = Trim(name);
        var trimmedTradingName = Trim(tradingName);

        if (trimmedName is null && trimmedTradingName is null && taxId is null)
            throw LookupErrors.PartyWithoutAnyIdentifier();

        return new LookupParty
        {
            Name = trimmedName,
            TradingName = trimmedTradingName,
            TaxId = taxId,
        };
    }

    /// <summary>
    /// Compõe a parte a partir dos valores crus do provedor, tolerando documento ilegível.
    /// </summary>
    /// <remarks>
    /// Documento que não passa no dígito verificador vira ausência, não exceção: um CNPJ
    /// corrompido na resposta do provedor não pode derrubar a consulta inteira — sem ele o
    /// check cai para o cotejo por nome, que é o comportamento já previsto para arrecadação.
    /// </remarks>
    public static LookupParty From(string? name, string? tradingName, string? taxId)
        => Of(name, tradingName, SharedKernel.TaxId.TryParse(taxId, out var parsed) ? parsed : null);

    public bool HasTaxId => TaxId is not null;

    /// <summary>Nome fantasia quando houver, senão a razão social — a ordem que a tela de aprovação usa.</summary>
    public string? DisplayName => Name ?? TradingName;

    private static string? Trim(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length > NAME_MAX_LENGTH ? trimmed[..NAME_MAX_LENGTH] : trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return TradingName;
        yield return TaxId;
    }
}
