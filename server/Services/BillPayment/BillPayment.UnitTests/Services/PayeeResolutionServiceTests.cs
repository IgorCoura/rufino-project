namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.Lookups;
using BillPayment.Domain.Services;
using BillPayment.UnitTests.Bills.Mothers;
using BillPayment.UnitTests.Lookups.Mothers;
using BillPayment.UnitTests.Services.Mothers;

public class PayeeResolutionServiceTests
{
    private const string OtherCnpj = "11444777000161";

    // Documento é o sinal forte e vem primeiro: casa mesmo com o nome escrito de outro jeito.
    [Fact]
    public void Resolve_WhenTheTaxIdMatches_ShouldMatchByTaxIdEvenWithADifferentName()
    {
        var payee = ValidationMother.RegisteredPayee(legalName: "OUTRO NOME COMPLETAMENTE DIFERENTE");

        var resolution = PayeeResolutionService.Resolve(
            LookupParty.From(LookupMother.BENEFICIARY_NAME, null, LookupMother.BENEFICIARY_CNPJ),
            [payee]);

        Assert.Equal(PayeeMatchKind.ByTaxId, resolution.Kind);
        Assert.Same(payee, resolution.Payee);
    }

    // Sem documento — o caso de 100% da arrecadação — sobra o nome, e ele casa.
    [Fact]
    public void Resolve_WithoutATaxId_ShouldFallBackToTheName()
    {
        var payee = ValidationMother.RegisteredPayee(legalName: LookupMother.UTILITY_COMPANY_NAME);

        var resolution = PayeeResolutionService.Resolve(
            LookupParty.From(null, LookupMother.UTILITY_COMPANY_NAME, null),
            [payee]);

        Assert.Equal(PayeeMatchKind.ByName, resolution.Kind);
    }

    // Beneficiário novo não é falha — é rotina, e a resolução simplesmente não encontra.
    [Fact]
    public void Resolve_WithNoCandidateAtAll_ShouldReturnNotFound()
    {
        var resolution = PayeeResolutionService.Resolve(
            LookupParty.From("FORNECEDOR NOVO LTDA", null, OtherCnpj),
            []);

        Assert.Equal(PayeeMatchKind.NotFound, resolution.Kind);
    }

    // O cenário de fraude de boleto: nome do fornecedor conhecido, CNPJ de outra pessoa.
    // É o que justifica a severidade bloqueante do check de beneficiário.
    [Fact]
    public void Resolve_WithAKnownNameButAnotherTaxId_ShouldFlagLookalike()
    {
        var payee = ValidationMother.RegisteredPayee();

        var resolution = PayeeResolutionService.Resolve(
            LookupParty.From(LookupMother.BENEFICIARY_NAME, null, OtherCnpj),
            [payee]);

        Assert.Equal(PayeeMatchKind.Lookalike, resolution.Kind);
        Assert.Equal(payee.LegalName, resolution.LookalikeName);
    }

    // Sem documento na consulta não existe "outro CNPJ com o mesmo nome" — existe apenas um
    // nome que ainda não casou. Marcar sósia aqui bloquearia todo beneficiário novo de
    // arrecadação cujo nome lembrasse um cadastrado.
    [Fact]
    public void Resolve_WithoutATaxId_ShouldNeverFlagLookalike()
    {
        var payee = ValidationMother.RegisteredPayee();

        var resolution = PayeeResolutionService.Resolve(
            LookupParty.From("PADARIA SAO JOSE LTDAA", null, null),
            [payee]);

        Assert.Equal(PayeeMatchKind.NotFound, resolution.Kind);
    }

    // Nome sem nenhuma semelhança e documento diferente é beneficiário novo, não sósia.
    [Fact]
    public void Resolve_WithAnUnrelatedName_ShouldReturnNotFoundInsteadOfLookalike()
    {
        var resolution = PayeeResolutionService.Resolve(
            LookupParty.From("TRANSPORTADORA ZZZ", null, OtherCnpj),
            [ValidationMother.RegisteredPayee()]);

        Assert.Equal(PayeeMatchKind.NotFound, resolution.Kind);
    }

    // A comparação ignora acento, pontuação e caixa — senão "PADARIA SÃO JOSÉ" e
    // "PADARIA SAO JOSE" pareceriam nomes distintos e o sósia passaria batido.
    [Fact]
    public void Similarity_ShouldIgnoreAccentsPunctuationAndCase()
    {
        Assert.Equal(1d, PayeeResolutionService.Similarity("Padaria São José Ltda.", "PADARIA SAO JOSE LTDA"));
    }

    // Nomes sem relação ficam bem abaixo do limiar de suspeita.
    [Fact]
    public void Similarity_WithUnrelatedNames_ShouldStayBelowTheLookalikeThreshold()
    {
        var score = PayeeResolutionService.Similarity("PADARIA SAO JOSE", "TRANSPORTADORA ZZZ");

        Assert.True(score < PayeeResolutionService.LOOKALIKE_SIMILARITY_THRESHOLD);
    }

    // Apelido aprendido também conta na detecção de sósia: o fraudador copia o nome que o
    // usuário vê, e esse pode ser o apelido, não a razão social.
    [Fact]
    public void Resolve_WhenTheNameResemblesALearnedAlias_ShouldFlagLookalike()
    {
        var payee = ValidationMother.RegisteredPayee(legalName: "CIA SANEAMENTO BASICO EST SP");
        payee.LearnAlias("SABESP SANEAMENTO", BillMother.DefaultOccurredAt);

        var resolution = PayeeResolutionService.Resolve(
            LookupParty.From("SABESP SANEAMENT0", null, OtherCnpj),
            [payee]);

        Assert.Equal(PayeeMatchKind.Lookalike, resolution.Kind);
    }
}
