namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.Extraction;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// A derivação de senha do degrau 0 do doc 09.
/// </summary>
/// <remarks>
/// O achado que a justifica: a senha é <strong>prova de propriedade</strong> — o emissor a
/// derivou do documento do pagador, então abrir com um documento do tenant é evidência de que o
/// boleto é dele.
/// </remarks>
public class PasswordDerivationServiceTests
{
    private static readonly DateTime OccurredAt = new(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));

    private const string Cnpj = "11222333000181";
    private const string Cpf = "52998224725";

    private static PayerProfile CompanyProfile(params string[] additional)
    {
        var profile = PayerProfile.Register(Tenant, PayerKind.Company, "RUFINO TESTE LTDA", Cnpj, OccurredAt);

        foreach (var taxId in additional)
            profile.AddAdditionalTaxId(taxId, OccurredAt);

        return profile;
    }

    // De um CNPJ saem os cinco formatos que os emissores usam, do prefixo mais curto ao documento
    // inteiro. 3 e 4 entraram em 2026-09-10, contra documento real do acervo.
    [Fact]
    public void Derive_ForCompany_ShouldProduceTheFiveCnpjFormatsInOrder()
    {
        var candidates = PasswordDerivationService.Derive(CompanyProfile());

        Assert.Equal(
            ["112", "1122", "11222", "11222333", Cnpj],
            candidates.Select(c => c.Value).ToArray());
    }

    // REGRESSÃO (2026-09-10, achado do usuário): boleto cifrado cuja senha são os QUATRO primeiros
    // dígitos do CNPJ. Sem o prefixo na lista a senha certa nunca era oferecida ao parser, e o
    // documento virava pdf_locked por ausência de candidata — não por senha errada.
    [Fact]
    public void Derive_ForCompany_ShouldOfferTheFourDigitPrefix()
    {
        var candidates = PasswordDerivationService.Derive(CompanyProfile());

        var fourDigits = Assert.Single(candidates, c => c.DerivedFrom == "cnpj_first_4_primary");
        Assert.Equal("1122", fourDigits.Value);
    }

    // A régua de prova forte do degrau 0: prefixo curto abre o documento, mas não sustenta sozinho
    // a atribuição ao tenant — 3 dígitos são mil possibilidades.
    [Theory]
    [InlineData("cnpj_first_3_primary", true)]
    [InlineData("cnpj_first_4_primary", true)]
    [InlineData("cnpj_first_5_primary", false)]
    [InlineData("cnpj_first_8_primary", false)]
    [InlineData("cnpj_full_primary", false)]
    [InlineData("cpf_first_4_additional_1", true)]
    [InlineData("cpf_first_11_additional_1", false)]
    [InlineData("empty", false)]
    [InlineData(null, false)]
    public void IsShortPrefixProof_ShouldOnlyFlagPrefixesBelowTheThreshold(string? label, bool expected)
        => Assert.Equal(expected, PasswordCandidate.IsShortPrefixProof(label));

    // Todo rótulo produzido pela derivação é legível pela régua que o consome — o acoplamento que
    // a LabelForDocument existe para garantir.
    [Fact]
    public void Derive_ShouldProduceLabelsTheStrengthRulerUnderstands()
    {
        var candidates = PasswordDerivationService.Derive(CompanyProfile(Cpf));

        // Os prefixos curtos são exatamente os dois primeiros de cada documento.
        var shortOnes = candidates
            .Where(c => PasswordCandidate.IsShortPrefixProof(c.DerivedFrom))
            .Select(c => c.Value)
            .ToArray();

        Assert.Equal(["112", "1122", "529", "5299"], shortOnes);
    }

    // Cada candidata carrega o rótulo do campo que a gerou — é ele, e nunca a senha, que vira
    // evidência no CaptureItem.UnlockedBy.
    [Fact]
    public void Derive_ShouldLabelEveryCandidateWithItsOrigin()
    {
        var candidates = PasswordDerivationService.Derive(CompanyProfile());

        Assert.All(candidates, c => Assert.False(string.IsNullOrWhiteSpace(c.DerivedFrom)));
        Assert.Contains(candidates, c => c.DerivedFrom == "cnpj_first_5_primary");
        Assert.Contains(candidates, c => c.DerivedFrom == "cnpj_full_primary");
    }

    // CPF tem cinco formatos, e o de 6 dígitos cobre o padrão de algumas concessionárias.
    [Fact]
    public void Derive_ForIndividual_ShouldProduceTheFiveCpfFormats()
    {
        var profile = PayerProfile.Register(Tenant, PayerKind.Individual, "IGOR TESTE", Cpf, OccurredAt);

        var candidates = PasswordDerivationService.Derive(profile);

        Assert.Equal(
            ["529", "5299", "52998", "529982", Cpf],
            candidates.Select(c => c.Value).ToArray());
    }

    // Documentos adicionais entram depois do principal: filial, ou o CPF do titular junto do
    // CNPJ no caso do MEI — o emissor pode ter usado qualquer um.
    [Fact]
    public void Derive_ShouldIncludeAdditionalTaxIdsAfterThePrimary()
    {
        var candidates = PasswordDerivationService.Derive(CompanyProfile(Cpf));

        Assert.Contains(candidates, c => c.Value == Cpf);
        Assert.Contains(candidates, c => c.DerivedFrom.EndsWith("_additional_0", StringComparison.Ordinal));

        // O principal continua vindo antes — é dele que a senha sai na maioria dos casos.
        Assert.EndsWith("_primary", candidates[0].DerivedFrom, StringComparison.Ordinal);
        Assert.StartsWith(candidates[0].Value, Cnpj, StringComparison.Ordinal);
    }

    // Duas filiais com a mesma raiz gerariam a mesma candidata; tentar duas vezes só gastaria o
    // teto de tentativas sem aumentar a chance de abrir.
    [Fact]
    public void Derive_WithSiblingBranches_ShouldNotRepeatTheSharedRoot()
    {
        // Mesma raiz (11222333), filial diferente.
        var candidates = PasswordDerivationService.Derive(CompanyProfile("11222333000262"));

        var values = candidates.Select(c => c.Value).ToArray();

        Assert.Equal(values.Length, values.Distinct(StringComparer.Ordinal).Count());
        Assert.Single(values, v => v == "11222333");
    }

    // Sem perfil cadastrado não há de onde derivar — e isso não é erro, é o tenant que ainda não
    // concluiu o onboarding.
    [Fact]
    public void Derive_WithoutProfile_ShouldReturnEmpty()
        => Assert.Empty(PasswordDerivationService.Derive(profile: null));
}
