namespace BillPayment.UnitTests.TrustedOrigins;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.TrustedOrigins;
using BillPayment.UnitTests.TrustedOrigins.Mothers;

public class TrustedOriginTests
{
    private static readonly DateTime Later = TrustedOriginMother.DefaultOccurredAt.AddDays(3);

    // Registrar um endereço confiável guarda tipo, valor, decisão, autor e instante.
    [Fact]
    public void Register_WithValidEmailAddress_ShouldStoreDecisionAndAudit()
    {
        var origin = TrustedOriginMother.TrustedAddress();

        Assert.Same(OriginKind.EmailAddress, origin.Kind);
        Assert.Same(TrustDecision.Trusted, origin.Decision);
        Assert.Equal(TrustedOriginMother.DefaultAddress, origin.Value);
        Assert.Equal(TrustedOriginMother.DefaultTenant, origin.TenantId);
        Assert.Equal(TrustedOriginMother.DefaultDecidedBy, origin.DecidedBy);
        Assert.Equal(TrustedOriginMother.DefaultOccurredAt, origin.DecidedAt);
        Assert.Equal(TrustedOriginMother.DefaultOccurredAt, origin.CreatedAt);
        Assert.Equal(TrustedOriginMother.DefaultOccurredAt, origin.UpdatedAt);
    }

    // O valor é normalizado para minúsculas e sem espaços nas bordas ao registrar.
    [Theory]
    [InlineData("  FINANCEIRO@Fornecedor.COM.BR  ", "financeiro@fornecedor.com.br")]
    [InlineData("Financeiro@Fornecedor.com.br", "financeiro@fornecedor.com.br")]
    public void Register_WithUnnormalizedValue_ShouldStoreNormalizedValue(string input, string expected)
    {
        var origin = TrustedOriginMother.Register(OriginKind.EmailAddress, input);

        Assert.Equal(expected, origin.Value);
    }

    // Domínio de e-mail e domínio web aceitam valor sem '@'.
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Register_WithDomainKind_ShouldAcceptValueWithoutAtSign(int kindId)
    {
        var kind = Enumeration.FromValue<OriginKind>(kindId);

        var origin = TrustedOriginMother.Register(kind, TrustedOriginMother.DefaultDomain);

        Assert.Equal(TrustedOriginMother.DefaultDomain, origin.Value);
    }

    // Observação em branco é guardada como ausente, não como string vazia.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithBlankNote_ShouldStoreNull(string? note)
    {
        var origin = TrustedOriginMother.Register(note: note);

        Assert.Null(origin.Note);
    }

    // Observação preenchida é guardada sem espaços nas bordas.
    [Fact]
    public void Register_WithNote_ShouldTrimAndStoreIt()
    {
        var origin = TrustedOriginMother.Register(note: "  confirmado por telefone  ");

        Assert.Equal("confirmado por telefone", origin.Note);
    }

    // Tipo de origem ausente impede o cadastro — BLP.ORG03.
    [Fact]
    public void Register_WithoutKind_ShouldThrow_BLP_ORG03()
    {
        var ex = Assert.Throws<DomainException>(() => TrustedOriginMother.RegisterVerbatim(
            null!, TrustedOriginMother.DefaultAddress, TrustDecision.Trusted, TrustedOriginMother.DefaultDecidedBy));

        Assert.Equal("BLP.ORG03", ex.Id);
    }

    // Decisão de confiança ausente impede o cadastro — BLP.ORG04.
    [Fact]
    public void Register_WithoutDecision_ShouldThrow_BLP_ORG04()
    {
        var ex = Assert.Throws<DomainException>(() => TrustedOriginMother.RegisterVerbatim(
            OriginKind.EmailAddress, TrustedOriginMother.DefaultAddress, null!, TrustedOriginMother.DefaultDecidedBy));

        Assert.Equal("BLP.ORG04", ex.Id);
    }

    // Valor vazio ou só espaços impede o cadastro — BLP.ORG05.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithBlankValue_ShouldThrow_BLP_ORG05(string value)
    {
        var ex = Assert.Throws<DomainException>(() => TrustedOriginMother.Register(value: value));

        Assert.Equal("BLP.ORG05", ex.Id);
    }

    // Valor acima do limite de caracteres impede o cadastro — BLP.ORG06.
    [Fact]
    public void Register_WithOversizedValue_ShouldThrow_BLP_ORG06()
    {
        var oversized = new string('a', TrustedOrigin.VALUE_MAX_LENGTH) + "@fornecedor.com.br";

        var ex = Assert.Throws<DomainException>(() => TrustedOriginMother.Register(value: oversized));

        Assert.Equal("BLP.ORG06", ex.Id);
    }

    // Endereço de e-mail malformado impede o cadastro — BLP.ORG07.
    [Theory]
    [InlineData("sem-arroba.com.br")]
    [InlineData("@fornecedor.com.br")]
    [InlineData("financeiro@")]
    [InlineData("financeiro@fornecedor")]
    [InlineData("dois@arrobas@fornecedor.com.br")]
    [InlineData("financeiro@.com.br")]
    [InlineData("financeiro@fornecedor..com.br")]
    public void Register_WithMalformedEmailAddress_ShouldThrow_BLP_ORG07(string value)
    {
        var ex = Assert.Throws<DomainException>(
            () => TrustedOriginMother.Register(OriginKind.EmailAddress, value));

        Assert.Equal("BLP.ORG07", ex.Id);
    }

    // Domínio malformado impede o cadastro — BLP.ORG08.
    [Theory]
    [InlineData("fornecedor")]
    [InlineData("com@fornecedor.com.br")]
    [InlineData(".fornecedor.com.br")]
    [InlineData("fornecedor.com.br.")]
    [InlineData("-fornecedor.com.br")]
    [InlineData("fornecedor-.com.br")]
    [InlineData("fornecedor..com.br")]
    [InlineData("forne cedor.com.br")]
    public void Register_WithMalformedDomain_ShouldThrow_BLP_ORG08(string value)
    {
        var ex = Assert.Throws<DomainException>(
            () => TrustedOriginMother.Register(OriginKind.EmailDomain, value));

        Assert.Equal("BLP.ORG08", ex.Id);
    }

    // Observação acima do limite impede o cadastro — BLP.ORG09.
    [Fact]
    public void Register_WithOversizedNote_ShouldThrow_BLP_ORG09()
    {
        var oversized = new string('x', TrustedOrigin.NOTE_MAX_LENGTH + 1);

        var ex = Assert.Throws<DomainException>(() => TrustedOriginMother.Register(note: oversized));

        Assert.Equal("BLP.ORG09", ex.Id);
    }

    // Decisão precisa de autor: UserId vazio impede o cadastro — BLP.ORG10.
    [Fact]
    public void Register_WithoutDecidedBy_ShouldThrow_BLP_ORG10()
    {
        var ex = Assert.Throws<DomainException>(
            () => TrustedOriginMother.Register(decidedBy: UserId.Empty));

        Assert.Equal("BLP.ORG10", ex.Id);
    }

    // Promover uma origem para confiável troca decisão, autor, instante e UpdatedAt.
    [Fact]
    public void ChangeDecision_FromBlockedToTrusted_ShouldReplaceDecisionAndAudit()
    {
        var origin = TrustedOriginMother.BlockedAddress();
        var otherUser = UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000b2"));

        origin.ChangeDecision(TrustDecision.Trusted, otherUser, "revisado", Later);

        Assert.Same(TrustDecision.Trusted, origin.Decision);
        Assert.Equal(otherUser, origin.DecidedBy);
        Assert.Equal(Later, origin.DecidedAt);
        Assert.Equal(Later, origin.UpdatedAt);
        Assert.Equal("revisado", origin.Note);
    }

    // Mudar a decisão sem observação limpa a observação anterior.
    [Fact]
    public void ChangeDecision_WithoutNote_ShouldClearPreviousNote()
    {
        var origin = TrustedOriginMother.Register(note: "observação antiga");

        origin.ChangeDecision(TrustDecision.Blocked, TrustedOriginMother.DefaultDecidedBy, null, Later);

        Assert.Null(origin.Note);
    }

    // Mudar a decisão para um valor nulo é recusado — BLP.ORG04.
    [Fact]
    public void ChangeDecision_WithoutDecision_ShouldThrow_BLP_ORG04()
    {
        var origin = TrustedOriginMother.TrustedAddress();

        var ex = Assert.Throws<DomainException>(
            () => origin.ChangeDecision(null!, TrustedOriginMother.DefaultDecidedBy, null, Later));

        Assert.Equal("BLP.ORG04", ex.Id);
    }

    // Origem cadastrada como endereço casa só com o endereço exato, não com o domínio.
    [Theory]
    [InlineData("financeiro@fornecedor.com.br", true)]
    [InlineData("  FINANCEIRO@FORNECEDOR.COM.BR ", true)]
    [InlineData("outro@fornecedor.com.br", false)]
    [InlineData("fornecedor.com.br", false)]
    public void Matches_WhenKindIsEmailAddress_ShouldRequireExactAddress(string sender, bool expected)
    {
        var origin = TrustedOriginMother.TrustedAddress();

        Assert.Equal(expected, origin.Matches(sender));
    }

    // Origem cadastrada como domínio casa com qualquer remetente daquele domínio.
    [Theory]
    [InlineData("financeiro@fornecedor.com.br", true)]
    [InlineData("cobranca@fornecedor.com.br", true)]
    [InlineData("fornecedor.com.br", true)]
    [InlineData("financeiro@outro.com.br", false)]
    [InlineData("fornecedor.com", false)]
    public void Matches_WhenKindIsEmailDomain_ShouldMatchAnyAddressOfThatDomain(string sender, bool expected)
    {
        var origin = TrustedOriginMother.TrustedDomain();

        Assert.Equal(expected, origin.Matches(sender));
    }

    // Remetente em branco nunca casa com origem alguma.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Matches_WithBlankSender_ShouldReturnFalse(string sender)
    {
        var origin = TrustedOriginMother.TrustedAddress();

        Assert.False(origin.Matches(sender));
    }

    // Normalize é a chave canônica: mesma saída para variações de caixa e espaços.
    [Theory]
    [InlineData("  Fornecedor.COM.BR ", "fornecedor.com.br")]
    [InlineData("FINANCEIRO@X.COM", "financeiro@x.com")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Normalize_ShouldProduceCanonicalKey(string input, string expected)
    {
        Assert.Equal(expected, TrustedOrigin.Normalize(input));
    }

    // ExtractDomain devolve o domínio normalizado de um endereço, ou vazio quando não há.
    [Theory]
    [InlineData("FINANCEIRO@Fornecedor.com.br", "fornecedor.com.br")]
    [InlineData("fornecedor.com.br", "")]
    [InlineData("financeiro@", "")]
    [InlineData("", "")]
    public void ExtractDomain_ShouldReturnNormalizedDomainOrEmpty(string input, string expected)
    {
        Assert.Equal(expected, TrustedOrigin.ExtractDomain(input));
    }
}
