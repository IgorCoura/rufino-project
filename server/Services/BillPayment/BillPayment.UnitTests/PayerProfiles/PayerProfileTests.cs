namespace BillPayment.UnitTests.PayerProfiles;

using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.PayerProfiles.Mothers;

public class PayerProfileTests
{
    private static readonly DateTime Later = PayerProfileMother.DefaultOccurredAt.AddDays(3);

    // A sobrecarga por primitivos deduz o tipo do documento a partir dos dígitos.
    [Fact]
    public void Register_FromPrimitives_ShouldParseThePrimaryTaxId()
    {
        var profile = PayerProfile.Register(
            PayerProfileMother.DefaultTenant,
            PayerKind.Company,
            "Rufino Empreiteira",
            "11.222.333/0001-81",
            PayerProfileMother.DefaultOccurredAt);

        Assert.Equal("11222333000181", profile.PrimaryTaxId.Value);
        Assert.Same(TaxIdKind.CNPJ, profile.PrimaryTaxId.Kind);
    }

    // A dedução do tipo não dispensa a conferência contra o tipo de pagador — BLP.PRF02.
    [Fact]
    public void Register_FromPrimitives_WithCpfForCompany_ShouldThrow_BLP_PRF02()
    {
        var ex = Assert.Throws<DomainException>(() => PayerProfile.Register(
            PayerProfileMother.DefaultTenant,
            PayerKind.Company,
            "Rufino Empreiteira",
            "529.982.247-25",
            PayerProfileMother.DefaultOccurredAt));

        Assert.Equal("BLP.PRF02", ex.Id);
    }

    // SetCnpjRootMatching concentra o liga-desliga numa única porta do agregado.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetCnpjRootMatching_ShouldDriveTheAggregateToTheRequestedState(bool enabled)
    {
        var profile = PayerProfileMother.Register();

        profile.SetCnpjRootMatching(enabled, Later);

        Assert.Equal(enabled, profile.MatchByCnpjRoot);
    }

    // Ligar o casamento por raiz numa pessoa física continua reprovado — BLP.PRF07.
    [Fact]
    public void SetCnpjRootMatching_WhenEnablingForIndividual_ShouldThrow_BLP_PRF07()
    {
        var profile = PayerProfileMother.Individual();

        var ex = Assert.Throws<DomainException>(() => profile.SetCnpjRootMatching(true, Later));

        Assert.Equal("BLP.PRF07", ex.Id);
    }

    // Cadastrar uma PJ guarda natureza, nome e documento principal.
    [Fact]
    public void Register_AsCompany_ShouldStoreKindNameAndPrimaryTaxId()
    {
        var profile = PayerProfileMother.Register();

        Assert.Same(PayerKind.Company, profile.Kind);
        Assert.Equal(PayerProfileMother.DefaultLegalName, profile.LegalName);
        Assert.Equal(PayerProfileMother.HeadquartersCnpj, profile.PrimaryTaxId.Value);
        Assert.Equal(PayerProfileMother.DefaultTenant, profile.TenantId);
        Assert.Empty(profile.AdditionalTaxIds);
        Assert.False(profile.MatchByCnpjRoot);
        Assert.Equal(PayerProfileMother.DefaultOccurredAt, profile.CreatedAt);
    }

    // Cadastrar uma PF guarda o CPF como documento principal.
    [Fact]
    public void Register_AsIndividual_ShouldAcceptCpfAsPrimary()
    {
        var profile = PayerProfileMother.Individual();

        Assert.Same(PayerKind.Individual, profile.Kind);
        Assert.Same(TaxIdKind.CPF, profile.PrimaryTaxId.Kind);
    }

    // PJ com CPF como documento principal é recusada — BLP.PRF02.
    [Fact]
    public void Register_AsCompanyWithCpf_ShouldThrow_BLP_PRF02()
    {
        var ex = Assert.Throws<DomainException>(() => PayerProfileMother.RegisterVerbatim(
            PayerKind.Company, PayerProfileMother.DefaultLegalName, PayerProfileMother.Cpf()));

        Assert.Equal("BLP.PRF02", ex.Id);
    }

    // PF com CNPJ como documento principal é recusada — BLP.PRF02.
    [Fact]
    public void Register_AsIndividualWithCnpj_ShouldThrow_BLP_PRF02()
    {
        var ex = Assert.Throws<DomainException>(() => PayerProfileMother.RegisterVerbatim(
            PayerKind.Individual, PayerProfileMother.DefaultLegalName, PayerProfileMother.Cnpj()));

        Assert.Equal("BLP.PRF02", ex.Id);
    }

    // Documento principal ausente impede o cadastro — BLP.PRF01.
    [Fact]
    public void Register_WithoutPrimaryTaxId_ShouldThrow_BLP_PRF01()
    {
        var ex = Assert.Throws<DomainException>(() => PayerProfileMother.RegisterVerbatim(
            PayerKind.Company, PayerProfileMother.DefaultLegalName, null!));

        Assert.Equal("BLP.PRF01", ex.Id);
    }

    // Nome vazio impede o cadastro — BLP.PRF05.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithBlankLegalName_ShouldThrow_BLP_PRF05(string legalName)
    {
        var ex = Assert.Throws<DomainException>(() => PayerProfileMother.Register(legalName: legalName));

        Assert.Equal("BLP.PRF05", ex.Id);
    }

    // Nome acima do limite impede o cadastro — BLP.PRF06.
    [Fact]
    public void Register_WithOversizedLegalName_ShouldThrow_BLP_PRF06()
    {
        var oversized = new string('a', PayerProfile.LEGAL_NAME_MAX_LENGTH + 1);

        var ex = Assert.Throws<DomainException>(() => PayerProfileMother.Register(legalName: oversized));

        Assert.Equal("BLP.PRF06", ex.Id);
    }

    // Acrescentar uma filial a torna reconhecida como documento próprio.
    [Fact]
    public void AddAdditionalTaxId_WithBranch_ShouldBeRecognizedAsOwned()
    {
        var profile = PayerProfileMother.Register();
        var branch = PayerProfileMother.Cnpj(PayerProfileMother.BranchCnpj);

        profile.AddAdditionalTaxId(branch, Later);

        Assert.Contains(branch, profile.AdditionalTaxIds);
        Assert.True(profile.Owns(branch));
        Assert.Equal(Later, profile.UpdatedAt);
    }

    // Acrescentar a mesma filial duas vezes é idempotente.
    [Fact]
    public void AddAdditionalTaxId_Twice_ShouldBeIdempotent()
    {
        var profile = PayerProfileMother.Register();
        profile.AddAdditionalTaxId(PayerProfileMother.Cnpj(PayerProfileMother.BranchCnpj), Later);
        var afterFirst = profile.UpdatedAt;

        profile.AddAdditionalTaxId(PayerProfileMother.Cnpj(PayerProfileMother.BranchCnpj), Later.AddDays(1));

        Assert.Single(profile.AdditionalTaxIds);
        Assert.Equal(afterFirst, profile.UpdatedAt);
    }

    // O documento principal não pode ser cadastrado também como adicional — BLP.PRF09.
    [Fact]
    public void AddAdditionalTaxId_WithPrimaryTaxId_ShouldThrow_BLP_PRF09()
    {
        var profile = PayerProfileMother.Register();

        var ex = Assert.Throws<DomainException>(
            () => profile.AddAdditionalTaxId(PayerProfileMother.Cnpj(), Later));

        Assert.Equal("BLP.PRF09", ex.Id);
    }

    // Documento adicional ausente é recusado — BLP.PRF08. O cast fixa a sobrecarga do Value Object.
    [Fact]
    public void AddAdditionalTaxId_WithNull_ShouldThrow_BLP_PRF08()
    {
        var profile = PayerProfileMother.Register();

        var ex = Assert.Throws<DomainException>(() => profile.AddAdditionalTaxId((TaxId)null!, Later));

        Assert.Equal("BLP.PRF08", ex.Id);
    }

    // A sobrecarga por texto trata ausência como omissão do campo, não como formato inválido — BLP.PRF08.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddAdditionalTaxId_WithBlankText_ShouldThrow_BLP_PRF08(string? taxId)
    {
        var profile = PayerProfileMother.Register();

        var ex = Assert.Throws<DomainException>(() => profile.AddAdditionalTaxId(taxId!, Later));

        Assert.Equal("BLP.PRF08", ex.Id);
    }

    // Texto formatado e texto cru designam o mesmo documento — a sanitização é do VO.
    [Fact]
    public void AddAdditionalTaxId_WithFormattedText_ShouldBeRecognizedAsOwned()
    {
        var profile = PayerProfileMother.Register();

        profile.AddAdditionalTaxId("11.222.333/0002-62", Later);

        Assert.True(profile.Owns(new TaxId("11222333000262", TaxIdKind.CNPJ)));
    }

    // Remover uma filial a retira da lista e ela deixa de ser reconhecida.
    [Fact]
    public void RemoveAdditionalTaxId_WithKnownBranch_ShouldRemoveIt()
    {
        var profile = PayerProfileMother.Register();
        var branch = PayerProfileMother.Cnpj(PayerProfileMother.BranchCnpj);
        profile.AddAdditionalTaxId(branch, Later);

        profile.RemoveAdditionalTaxId(branch, Later.AddDays(1));

        Assert.Empty(profile.AdditionalTaxIds);
        Assert.False(profile.Owns(branch));
    }

    // O documento principal é sempre reconhecido como próprio.
    [Fact]
    public void Owns_WithPrimaryTaxId_ShouldReturnTrue()
    {
        var profile = PayerProfileMother.Register();

        Assert.True(profile.Owns(PayerProfileMother.Cnpj()));
    }

    // Documento de terceiro não é reconhecido como próprio.
    [Fact]
    public void Owns_WithForeignTaxId_ShouldReturnFalse()
    {
        var profile = PayerProfileMother.Register();

        Assert.False(profile.Owns(PayerProfileMother.Cnpj(PayerProfileMother.ForeignCnpj)));
    }

    // Documento ausente nunca é reconhecido como próprio.
    [Fact]
    public void Owns_WithNull_ShouldReturnFalse()
    {
        Assert.False(PayerProfileMother.Register().Owns(null!));
    }

    // Sem casamento por raiz, filial não cadastrada não é reconhecida.
    [Fact]
    public void Owns_WithBranchAndRootMatchingDisabled_ShouldReturnFalse()
    {
        var profile = PayerProfileMother.Register();

        Assert.False(profile.Owns(PayerProfileMother.Cnpj(PayerProfileMother.BranchCnpj)));
    }

    // Com casamento por raiz ligado, qualquer CNPJ da mesma raiz é reconhecido.
    [Fact]
    public void Owns_WithBranchAndRootMatchingEnabled_ShouldReturnTrue()
    {
        var profile = PayerProfileMother.CompanyWithRootMatching();
        var branch = PayerProfileMother.Cnpj(PayerProfileMother.BranchCnpj);

        Assert.True(profile.Owns(branch));
        Assert.True(profile.OwnsByCnpjRoot(branch));
    }

    // Casamento por raiz não estende o reconhecimento a CNPJ de raiz diferente.
    [Fact]
    public void OwnsByCnpjRoot_WithDifferentRoot_ShouldReturnFalse()
    {
        var profile = PayerProfileMother.CompanyWithRootMatching();

        Assert.False(profile.OwnsByCnpjRoot(PayerProfileMother.Cnpj(PayerProfileMother.ForeignCnpj)));
    }

    // O documento principal casa por igualdade, não por raiz — a evidência precisa distinguir os dois.
    [Fact]
    public void OwnsByCnpjRoot_WithPrimaryTaxId_ShouldStillReportRootMatch()
    {
        var profile = PayerProfileMother.CompanyWithRootMatching();

        Assert.True(profile.Owns(PayerProfileMother.Cnpj()));
        Assert.True(profile.OwnsByCnpjRoot(PayerProfileMother.Cnpj()));
    }

    // CPF nunca casa por raiz, mesmo com a opção ligada.
    [Fact]
    public void OwnsByCnpjRoot_WithCpfCandidate_ShouldReturnFalse()
    {
        var profile = PayerProfileMother.CompanyWithRootMatching();

        Assert.False(profile.OwnsByCnpjRoot(PayerProfileMother.Cpf()));
    }

    // Pessoa física não pode ligar casamento por raiz de CNPJ — BLP.PRF07.
    [Fact]
    public void EnableCnpjRootMatching_AsIndividual_ShouldThrow_BLP_PRF07()
    {
        var profile = PayerProfileMother.Individual();

        var ex = Assert.Throws<DomainException>(() => profile.EnableCnpjRootMatching(Later));

        Assert.Equal("BLP.PRF07", ex.Id);
    }

    // Desligar o casamento por raiz faz a filial deixar de ser reconhecida.
    [Fact]
    public void DisableCnpjRootMatching_ShouldStopRecognizingBranches()
    {
        var profile = PayerProfileMother.CompanyWithRootMatching();

        profile.DisableCnpjRootMatching(Later);

        Assert.False(profile.MatchByCnpjRoot);
        Assert.False(profile.Owns(PayerProfileMother.Cnpj(PayerProfileMother.BranchCnpj)));
    }

    // Ligar o casamento por raiz duas vezes é idempotente.
    [Fact]
    public void EnableCnpjRootMatching_WhenAlreadyEnabled_ShouldBeIdempotent()
    {
        var profile = PayerProfileMother.CompanyWithRootMatching();
        var before = profile.UpdatedAt;

        profile.EnableCnpjRootMatching(Later);

        Assert.Equal(before, profile.UpdatedAt);
    }

    // Sem subconta vinculada, o tenant não pode agendar pagamento.
    [Fact]
    public void CanSchedulePayments_WithoutAsaasAccount_ShouldBeFalse()
    {
        var profile = PayerProfileMother.Register();

        Assert.Null(profile.AsaasAccountRef);
        Assert.False(profile.CanSchedulePayments);
    }

    // Vincular a subconta guarda o PONTEIRO do cofre e libera o agendamento de pagamento.
    [Fact]
    public void LinkAsaasAccount_WithACredentialRef_ShouldEnableScheduling()
    {
        var profile = PayerProfileMother.Register();
        var credential = CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-00000000c0fe"));

        profile.LinkAsaasAccount(credential, Later);

        Assert.Equal(credential, profile.AsaasAccountRef);
        Assert.True(profile.CanSchedulePayments);
        Assert.Equal(Later, profile.UpdatedAt);
    }

    // Ponteiro nulo é recusado — BLP.PRF11: desvincular tem porta própria, não é "vincular nada".
    [Fact]
    public void LinkAsaasAccount_WithNull_ShouldThrow_BLP_PRF11()
    {
        var profile = PayerProfileMother.Register();

        var ex = Assert.Throws<DomainException>(() => profile.LinkAsaasAccount(null!, Later));

        Assert.Equal("BLP.PRF11", ex.Id);
    }

    // Desvincular limpa o ponteiro e trava o agendamento; repetir é inócuo.
    [Fact]
    public void UnlinkAsaasAccount_ShouldClearThePointerAndBeIdempotent()
    {
        var profile = PayerProfileMother.Register();
        profile.LinkAsaasAccount(
            CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-00000000c0fe")), Later);

        profile.UnlinkAsaasAccount(Later.AddDays(1));

        Assert.Null(profile.AsaasAccountRef);
        Assert.False(profile.CanSchedulePayments);

        var updatedAt = profile.UpdatedAt;
        profile.UnlinkAsaasAccount(Later.AddDays(2));
        Assert.Equal(updatedAt, profile.UpdatedAt);
    }

    // Renomear troca o nome e atualiza UpdatedAt.
    [Fact]
    public void Rename_WithNewName_ShouldReplaceLegalName()
    {
        var profile = PayerProfileMother.Register();

        profile.Rename("RUFINO EMPREITEIRA", Later);

        Assert.Equal("RUFINO EMPREITEIRA", profile.LegalName);
        Assert.Equal(Later, profile.UpdatedAt);
    }

    // A coleção exposta é somente-leitura — mutar de fora quebraria as invariantes do agregado.
    [Fact]
    public void AdditionalTaxIds_ShouldBeExposedAsReadOnly()
    {
        var profile = PayerProfileMother.Register();

        Assert.IsNotAssignableFrom<List<TaxId>>(profile.AdditionalTaxIds);
    }
}
