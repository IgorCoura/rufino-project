namespace BillPayment.UnitTests.Services;

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

    // De um CNPJ saem os três formatos que os emissores usam: 5 dígitos, a raiz e o documento
    // inteiro — nessa ordem, do mais comum para o menos.
    [Fact]
    public void Derive_ForCompany_ShouldProduceTheThreeCnpjFormatsInOrder()
    {
        var candidates = PasswordDerivationService.Derive(CompanyProfile());

        Assert.Equal(3, candidates.Count);
        Assert.Equal("11222", candidates[0].Value);
        Assert.Equal("11222333", candidates[1].Value);
        Assert.Equal(Cnpj, candidates[2].Value);
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

    // CPF tem quatro formatos, e o de 6 dígitos cobre o padrão de algumas concessionárias.
    [Fact]
    public void Derive_ForIndividual_ShouldProduceTheFourCpfFormats()
    {
        var profile = PayerProfile.Register(Tenant, PayerKind.Individual, "IGOR TESTE", Cpf, OccurredAt);

        var candidates = PasswordDerivationService.Derive(profile);

        Assert.Equal(4, candidates.Count);
        Assert.Equal(["529", "52998", "529982", Cpf], candidates.Select(c => c.Value).ToArray());
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
        Assert.StartsWith("11222", candidates[0].Value, StringComparison.Ordinal);
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
