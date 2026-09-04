namespace BillPayment.Domain.Services;

using BillPayment.Domain.Extraction;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Deriva as senhas candidatas de um PDF cifrado a partir do cadastro fiscal do tenant.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O achado que justifica isto: a senha é prova de propriedade.</strong> O emissor
/// derivou aquela senha do documento do pagador — se o PDF abre com um documento do
/// <c>PayerProfile</c>, isso é evidência forte, em muitos casos mais forte que OCR, de que o
/// boleto é daquele tenant (doc 09, degrau 0 do roteamento).
/// </para>
/// <para>
/// <strong>É derivação, não força bruta.</strong> Os candidatos saem de dados que o próprio
/// tenant cadastrou; o teto por documento vive na configuração do parser. Um PDF hostil não tem
/// como transformar isto num laço caro porque a lista é curta e finita por construção.
/// </para>
/// <para>
/// A ordem importa: prefixo curto antes de documento completo, porque é a forma que os emissores
/// mais usam. Cada candidata carrega o <strong>rótulo do campo</strong> que a gerou — é isso, e
/// nunca a senha, que vira evidência em <c>CaptureItem.UnlockedBy</c>.
/// </para>
/// </remarks>
public static class PasswordDerivationService
{
    /// <summary>Prefixos de CNPJ que os emissores usam. 8 é a raiz; 14 é o documento inteiro.</summary>
    private static readonly int[] CnpjPrefixes = [5, 8, 14];

    /// <summary>Prefixos de CPF. 6 cobre o formato "seis primeiros" de algumas concessionárias.</summary>
    private static readonly int[] CpfPrefixes = [3, 5, 6, 11];

    public static IReadOnlyList<PasswordCandidate> Derive(PayerProfile? profile)
    {
        if (profile is null)
            return [];

        var candidates = new List<PasswordCandidate>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        // O documento principal primeiro: é dele que vem a senha na esmagadora maioria dos casos.
        AddFor(profile.PrimaryTaxId, "primary", candidates, seen);

        // Filiais e o CPF do titular junto do CNPJ (MEI) — o emissor pode ter usado qualquer um.
        var index = 0;
        foreach (var taxId in profile.AdditionalTaxIds)
            AddFor(taxId, $"additional_{index++}", candidates, seen);

        return candidates;
    }

    private static void AddFor(
        TaxId taxId,
        string origin,
        List<PasswordCandidate> candidates,
        HashSet<string> seen)
    {
        if (taxId is null)
            return;

        var digits = taxId.Value;
        var isCnpj = taxId.Kind == TaxIdKind.CNPJ;
        var prefixes = isCnpj ? CnpjPrefixes : CpfPrefixes;
        var label = isCnpj ? "cnpj" : "cpf";

        foreach (var length in prefixes)
        {
            if (digits.Length < length)
                continue;

            var value = digits[..length];

            // Duas filiais com a mesma raiz de CNPJ gerariam a mesma candidata — tentar duas
            // vezes só gastaria o teto sem aumentar a chance de abrir.
            if (!seen.Add(value))
                continue;

            var suffix = length == digits.Length ? "full" : $"first_{length}";
            candidates.Add(PasswordCandidate.From(value, $"{label}_{suffix}_{origin}"));
        }
    }
}
