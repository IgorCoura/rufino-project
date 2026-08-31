namespace BillPayment.UnitTests.Payees;

using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.Payees.Mothers;

public class PayeeTests
{
    private static readonly DateTime Later = PayeeMother.DefaultOccurredAt.AddDays(3);

    // A sobrecarga por primitivos deduz o tipo do documento e monta a política de valor.
    [Fact]
    public void Register_FromPrimitives_ShouldParseTaxIdAndBuildPolicy()
    {
        var payee = Payee.Register(
            PayeeMother.DefaultTenant,
            "Energia do Vale S.A.",
            "11.222.333/0001-81",
            AmountPolicyKind.Range,
            null,
            null,
            80m,
            400m,
            PayeeMother.DefaultOccurredAt);

        Assert.Equal("11222333000181", payee.TaxId.Value);
        Assert.Same(TaxIdKind.CNPJ, payee.TaxId.Kind);
        Assert.Same(AmountPolicyKind.Range, payee.AmountPolicy.Kind);
        Assert.Equal(400m, payee.AmountPolicy.MaxAmount!.Amount);
        Assert.True(payee.IsActive);
    }

    // Trocar a política pela sobrecarga por primitivos substitui a expectativa de valor.
    [Fact]
    public void ChangeAmountPolicy_FromPrimitives_ShouldReplaceThePolicy()
    {
        var payee = PayeeMother.Register();

        payee.ChangeAmountPolicy(AmountPolicyKind.Fixed, 250m, 2m, null, null, Later);

        Assert.Same(AmountPolicyKind.Fixed, payee.AmountPolicy.Kind);
        Assert.Equal(250m, payee.AmountPolicy.ExpectedAmount!.Amount);
        Assert.Equal(Later, payee.UpdatedAt);
    }

    // SetActivation concentra o liga-desliga do cadastro numa única porta do agregado.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetActivation_ShouldDriveTheAggregateToTheRequestedState(bool isActive)
    {
        var payee = PayeeMother.Register();

        payee.SetActivation(isActive, Later);

        Assert.Equal(isActive, payee.IsActive);
    }

    // Reativar um beneficiário desativado devolve a capacidade de alterá-lo.
    [Fact]
    public void SetActivation_WhenReactivating_ShouldAllowChangesAgain()
    {
        var payee = PayeeMother.Register();
        payee.SetActivation(false, Later);

        payee.SetActivation(true, Later.AddDays(1));
        payee.Rename("Nome Novo", Later.AddDays(2));

        Assert.Equal("Nome Novo", payee.LegalName);
    }

    // A marca de confiança nasce Normal e muda pela porta única do agregado.
    [Fact]
    public void SetStanding_ShouldChangeTheTrustMark()
    {
        var payee = PayeeMother.Register();
        Assert.Same(PayeeStanding.Normal, payee.Standing);

        payee.SetStanding(PayeeStanding.Blacklisted, Later);

        Assert.Same(PayeeStanding.Blacklisted, payee.Standing);
        Assert.Equal(Later, payee.UpdatedAt);
    }

    // Marcar um mau ator precisa funcionar mesmo com o cadastro desativado — a marca fica
    // fora do guard de ativação, como o próprio liga-desliga.
    [Fact]
    public void SetStanding_OnAnInactivePayee_ShouldStillWork()
    {
        var payee = PayeeMother.Register();
        payee.SetActivation(false, Later);

        payee.SetStanding(PayeeStanding.Blacklisted, Later.AddDays(1));

        Assert.Same(PayeeStanding.Blacklisted, payee.Standing);
    }

    // Reaplicar a mesma marca é idempotente e não mexe no carimbo de atualização.
    [Fact]
    public void SetStanding_WithTheSameValue_ShouldBeIdempotent()
    {
        var payee = PayeeMother.Register();
        payee.SetStanding(PayeeStanding.Whitelisted, Later);

        payee.SetStanding(PayeeStanding.Whitelisted, Later.AddDays(5));

        Assert.Equal(Later, payee.UpdatedAt);
    }

    // Marca de confiança nula é recusada com BLP.PYE17.
    [Fact]
    public void SetStanding_WithNull_Throws_BLP_PYE17()
    {
        var payee = PayeeMother.Register();

        var ex = Assert.Throws<DomainException>(() => payee.SetStanding(null!, Later));

        Assert.Equal("BLP.PYE17", ex.Id);
    }

    // Cadastrar um beneficiário guarda nome, documento, política e nasce ativo.
    [Fact]
    public void Register_WithValidData_ShouldStoreDataAndBeActive()
    {
        var payee = PayeeMother.Register();

        Assert.Equal(PayeeMother.DefaultLegalName, payee.LegalName);
        Assert.Equal(PayeeMother.DefaultCnpj, payee.TaxId.Value);
        Assert.Same(AmountPolicyKind.Unbounded, payee.AmountPolicy.Kind);
        Assert.Equal(PayeeMother.DefaultTenant, payee.TenantId);
        Assert.True(payee.IsActive);
        Assert.Empty(payee.Aliases);
        Assert.Empty(payee.AcceptedBanks);
        Assert.Equal(PayeeMother.DefaultOccurredAt, payee.CreatedAt);
        Assert.Equal(PayeeMother.DefaultOccurredAt, payee.UpdatedAt);
    }

    // A razão social é guardada sem espaços nas bordas.
    [Fact]
    public void Register_WithPaddedLegalName_ShouldTrimIt()
    {
        var payee = PayeeMother.Register(legalName: "  SECONCI SAO PAULO  ");

        Assert.Equal("SECONCI SAO PAULO", payee.LegalName);
    }

    // Razão social vazia impede o cadastro — BLP.PYE03.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithBlankLegalName_ShouldThrow_BLP_PYE03(string legalName)
    {
        var ex = Assert.Throws<DomainException>(() => PayeeMother.Register(legalName: legalName));

        Assert.Equal("BLP.PYE03", ex.Id);
    }

    // Razão social acima do limite impede o cadastro — BLP.PYE04.
    [Fact]
    public void Register_WithOversizedLegalName_ShouldThrow_BLP_PYE04()
    {
        var oversized = new string('a', Payee.LEGAL_NAME_MAX_LENGTH + 1);

        var ex = Assert.Throws<DomainException>(() => PayeeMother.Register(legalName: oversized));

        Assert.Equal("BLP.PYE04", ex.Id);
    }

    // Documento fiscal ausente impede o cadastro — BLP.PYE05.
    [Fact]
    public void Register_WithoutTaxId_ShouldThrow_BLP_PYE05()
    {
        var ex = Assert.Throws<DomainException>(() => PayeeMother.RegisterVerbatim(
            PayeeMother.DefaultLegalName, null!, AmountPolicy.Unbounded()));

        Assert.Equal("BLP.PYE05", ex.Id);
    }

    // Política de valor ausente impede o cadastro — BLP.PYE06.
    [Fact]
    public void Register_WithoutAmountPolicy_ShouldThrow_BLP_PYE06()
    {
        var ex = Assert.Throws<DomainException>(() => PayeeMother.RegisterVerbatim(
            PayeeMother.DefaultLegalName, PayeeMother.Cnpj(), null!));

        Assert.Equal("BLP.PYE06", ex.Id);
    }

    // Renomear troca a razão social e atualiza UpdatedAt.
    [Fact]
    public void Rename_WithNewName_ShouldReplaceLegalNameAndTouchUpdatedAt()
    {
        var payee = PayeeMother.Register();

        payee.Rename("SECONCI SP", Later);

        Assert.Equal("SECONCI SP", payee.LegalName);
        Assert.Equal(Later, payee.UpdatedAt);
    }

    // Trocar a política de valor substitui a anterior.
    [Fact]
    public void ChangeAmountPolicy_WithNewPolicy_ShouldReplacePrevious()
    {
        var payee = PayeeMother.Register();

        payee.ChangeAmountPolicy(AmountPolicy.Fixed(PayeeMother.Brl(500m), 10m), Later);

        Assert.Same(AmountPolicyKind.Fixed, payee.AmountPolicy.Kind);
        Assert.Equal(500m, payee.AmountPolicy.ExpectedAmount!.Amount);
        Assert.Equal(Later, payee.UpdatedAt);
    }

    // Aprender um apelido o acrescenta à lista e atualiza UpdatedAt.
    [Fact]
    public void LearnAlias_WithNewAlias_ShouldAddItToAliases()
    {
        var payee = PayeeMother.Register();

        payee.LearnAlias("SERVICO SOCIAL DA CONSTRUCAO", Later);

        Assert.Contains("SERVICO SOCIAL DA CONSTRUCAO", payee.Aliases);
        Assert.Equal(Later, payee.UpdatedAt);
    }

    // Aprender um apelido já conhecido é idempotente e não mexe em UpdatedAt.
    [Fact]
    public void LearnAlias_WithKnownAlias_ShouldBeIdempotent()
    {
        var payee = PayeeMother.Register();
        payee.LearnAlias("SECONCI SP", Later);
        var afterFirst = payee.UpdatedAt;

        payee.LearnAlias("seconci sp", Later.AddDays(1));

        Assert.Single(payee.Aliases);
        Assert.Equal(afterFirst, payee.UpdatedAt);
    }

    // Aprender a própria razão social como apelido não duplica o nome.
    [Fact]
    public void LearnAlias_WithLegalName_ShouldNotDuplicate()
    {
        var payee = PayeeMother.Register();

        payee.LearnAlias(PayeeMother.DefaultLegalName, Later);

        Assert.Empty(payee.Aliases);
    }

    // Apelido vazio é recusado — BLP.PYE13.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void LearnAlias_WithBlankAlias_ShouldThrow_BLP_PYE13(string alias)
    {
        var payee = PayeeMother.Register();

        var ex = Assert.Throws<DomainException>(() => payee.LearnAlias(alias, Later));

        Assert.Equal("BLP.PYE13", ex.Id);
    }

    // Apelido acima do limite é recusado — BLP.PYE14.
    [Fact]
    public void LearnAlias_WithOversizedAlias_ShouldThrow_BLP_PYE14()
    {
        var payee = PayeeMother.Register();
        var oversized = new string('a', Payee.ALIAS_MAX_LENGTH + 1);

        var ex = Assert.Throws<DomainException>(() => payee.LearnAlias(oversized, Later));

        Assert.Equal("BLP.PYE14", ex.Id);
    }

    // Esquecer um apelido o remove da lista.
    [Fact]
    public void ForgetAlias_WithKnownAlias_ShouldRemoveIt()
    {
        var payee = PayeeMother.Register();
        payee.LearnAlias("SECONCI SP", Later);

        payee.ForgetAlias("seconci sp", Later.AddDays(1));

        Assert.Empty(payee.Aliases);
    }

    // MatchesName compara com a razão social e com os apelidos, sem distinção de caixa.
    [Theory]
    [InlineData("SECONCI SAO PAULO", true)]
    [InlineData("seconci sao paulo", true)]
    [InlineData("  SECONCI SAO PAULO  ", true)]
    [InlineData("SECONCI SP", true)]
    [InlineData("OUTRO FORNECEDOR", false)]
    [InlineData("", false)]
    public void MatchesName_ShouldConsiderLegalNameAndAliases(string candidate, bool expected)
    {
        var payee = PayeeMother.Register();
        payee.LearnAlias("SECONCI SP", Later);

        Assert.Equal(expected, payee.MatchesName(candidate));
    }

    // Aceitar um banco o acrescenta à lista de bancos recebedores.
    [Fact]
    public void AllowBank_WithNewBank_ShouldAddItToAcceptedBanks()
    {
        var payee = PayeeMother.Register();

        payee.AllowBank(new BankCode("033"), Later);

        Assert.Contains(new BankCode("033"), payee.AcceptedBanks);
        Assert.Equal(Later, payee.UpdatedAt);
    }

    // Aceitar um banco já aceito é idempotente.
    [Fact]
    public void AllowBank_WithKnownBank_ShouldBeIdempotent()
    {
        var payee = PayeeMother.Register();
        payee.AllowBank(new BankCode("033"), Later);
        var afterFirst = payee.UpdatedAt;

        payee.AllowBank(new BankCode("33"), Later.AddDays(1));

        Assert.Single(payee.AcceptedBanks);
        Assert.Equal(afterFirst, payee.UpdatedAt);
    }

    // Remover um banco aceito o retira da lista.
    [Fact]
    public void DisallowBank_WithAcceptedBank_ShouldRemoveIt()
    {
        var payee = PayeeMother.Register();
        payee.AllowBank(new BankCode("033"), Later);

        payee.DisallowBank(new BankCode("033"), Later.AddDays(1));

        Assert.Empty(payee.AcceptedBanks);
    }

    // Banco ausente é recusado — BLP.PYE15. O cast fixa a sobrecarga que recebe o Value Object.
    [Fact]
    public void AllowBank_WithoutBankCode_ShouldThrow_BLP_PYE15()
    {
        var payee = PayeeMother.Register();

        var ex = Assert.Throws<DomainException>(() => payee.AllowBank((BankCode)null!, Later));

        Assert.Equal("BLP.PYE15", ex.Id);
    }

    // A sobrecarga por texto trata ausência como omissão do campo, não como formato inválido — BLP.PYE15.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AllowBank_WithBlankBankCodeText_ShouldThrow_BLP_PYE15(string? bankCode)
    {
        var payee = PayeeMother.Register();

        var ex = Assert.Throws<DomainException>(() => payee.AllowBank(bankCode!, Later));

        Assert.Equal("BLP.PYE15", ex.Id);
    }

    // Texto de banco é normalizado pelo VO: "33" e "033" designam o mesmo banco recebedor.
    [Fact]
    public void AllowBank_WithUnpaddedBankCodeText_ShouldNormalizeToThreeDigits()
    {
        var payee = PayeeMother.Register();

        payee.AllowBank("33", Later);

        Assert.True(payee.AcceptsBank(new BankCode("033")));
    }

    // Remover banco pela sobrecarga de texto usa a mesma chave normalizada do cadastro.
    [Fact]
    public void DisallowBank_WithBankCodeText_ShouldRemoveTheNormalizedEntry()
    {
        var payee = PayeeMother.Register();
        payee.AllowBank(new BankCode("341"), Later);

        payee.DisallowBank("341", Later);

        Assert.Null(payee.AcceptsBank(new BankCode("341")));
    }

    // Sem banco cadastrado a resposta é nula — ausência de expectativa é inconclusiva, não reprovação.
    [Fact]
    public void AcceptsBank_WithoutAnyAcceptedBank_ShouldReturnNull()
    {
        var payee = PayeeMother.Register();

        Assert.Null(payee.AcceptsBank(new BankCode("341")));
    }

    // Com bancos cadastrados, responde verdadeiro só para os que estão na lista.
    [Theory]
    [InlineData("033", true)]
    [InlineData("33", true)]
    [InlineData("341", false)]
    public void AcceptsBank_WithAcceptedBanks_ShouldAnswerAgainstTheList(string candidate, bool expected)
    {
        var payee = PayeeMother.Register();
        payee.AllowBank(new BankCode("033"), Later);

        Assert.Equal(expected, payee.AcceptsBank(new BankCode(candidate)));
    }

    // Desativar marca o beneficiário como inativo.
    [Fact]
    public void Deactivate_WhenActive_ShouldMarkAsInactive()
    {
        var payee = PayeeMother.Register();

        payee.Deactivate(Later);

        Assert.False(payee.IsActive);
        Assert.Equal(Later, payee.UpdatedAt);
    }

    // Desativar duas vezes é idempotente e não mexe em UpdatedAt.
    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldBeIdempotent()
    {
        var payee = PayeeMother.Inactive();
        var before = payee.UpdatedAt;

        payee.Deactivate(Later);

        Assert.Equal(before, payee.UpdatedAt);
    }

    // Reativar devolve o beneficiário ao estado ativo e permite alterá-lo de novo.
    [Fact]
    public void Activate_WhenInactive_ShouldAllowChangesAgain()
    {
        var payee = PayeeMother.Inactive();

        payee.Activate(Later);
        payee.Rename("NOVO NOME", Later);

        Assert.True(payee.IsActive);
        Assert.Equal("NOVO NOME", payee.LegalName);
    }

    // Beneficiário inativo recusa qualquer alteração — BLP.PYE16.
    [Fact]
    public void Rename_WhenInactive_ShouldThrow_BLP_PYE16()
    {
        var payee = PayeeMother.Inactive();

        var ex = Assert.Throws<DomainException>(() => payee.Rename("OUTRO", Later));

        Assert.Equal("BLP.PYE16", ex.Id);
    }

    // A proteção de inatividade vale para todas as mutações, não só para renomear — BLP.PYE16.
    [Fact]
    public void MutatingMethods_WhenInactive_ShouldAllThrow_BLP_PYE16()
    {
        var payee = PayeeMother.Inactive();

        Assert.Equal("BLP.PYE16", Assert.Throws<DomainException>(
            () => payee.ChangeAmountPolicy(AmountPolicy.Unbounded(), Later)).Id);
        Assert.Equal("BLP.PYE16", Assert.Throws<DomainException>(
            () => payee.LearnAlias("X", Later)).Id);
        Assert.Equal("BLP.PYE16", Assert.Throws<DomainException>(
            () => payee.ForgetAlias("X", Later)).Id);
        Assert.Equal("BLP.PYE16", Assert.Throws<DomainException>(
            () => payee.AllowBank(new BankCode("033"), Later)).Id);
        Assert.Equal("BLP.PYE16", Assert.Throws<DomainException>(
            () => payee.DisallowBank(new BankCode("033"), Later)).Id);
    }

    // A coleção exposta é somente-leitura — mutar de fora quebraria as invariantes do agregado.
    [Fact]
    public void Aliases_ShouldBeExposedAsReadOnly()
    {
        var payee = PayeeMother.Register();

        Assert.IsNotAssignableFrom<List<string>>(payee.Aliases);
        Assert.IsNotAssignableFrom<List<BankCode>>(payee.AcceptedBanks);
    }
}
