namespace AccountsPayable.UnitTests.ExpenseClassificationRules.ValueObjects;

using AccountsPayable.Domain.ExpenseClassificationRules.ValueObjects;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;
using AccountsPayable.UnitTests.Payables.Mothers;

public class ClassificationMatcherTests
{
    private static readonly SupplierId OTHER_SUPPLIER =
        SupplierId.From(new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"));

    public class WhenConstructing
    {
        // Construtor sem nenhum critério lança AP.CMR01.
        [Fact]
        public void Construct_WithNoCriteria_ShouldThrowDomainException()
        {
            var ex = Assert.Throws<DomainException>(() => new ClassificationMatcher());

            Assert.Equal("AP.CMR01", ex.Id);
        }

        // SupplierId com Guid.Empty é tratado como "não definido" → ainda lança AP.CMR01 se for o único critério.
        [Fact]
        public void Construct_WithEmptySupplierIdAndNothingElse_ShouldThrowDomainException()
        {
            var ex = Assert.Throws<DomainException>(() => new ClassificationMatcher(supplierId: SupplierId.Empty));

            Assert.Equal("AP.CMR01", ex.Id);
        }

        // Keyword vazia/whitespace não conta como critério; precisa de outra para o matcher ser válido.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Construct_WithBlankKeywordAndNothingElse_ShouldThrowDomainException(string keyword)
        {
            var ex = Assert.Throws<DomainException>(() => new ClassificationMatcher(keyword: keyword));

            Assert.Equal("AP.CMR01", ex.Id);
        }

        // Faixa inválida (min > max) lança AP.CMR02.
        [Fact]
        public void Construct_WithMinAmountAboveMaxAmount_ShouldThrowDomainException()
        {
            var ex = Assert.Throws<DomainException>(() => new ClassificationMatcher(
                minAmount: new Money(2_000m, Currency.Brl),
                maxAmount: new Money(1_000m, Currency.Brl)));

            Assert.Equal("AP.CMR02", ex.Id);
        }

        // Keyword acima do limite (KEYWORD_MAX_LENGTH = 200) lança AP.CMR03.
        [Fact]
        public void Construct_WithKeywordAboveMaxLength_ShouldThrowDomainException()
        {
            var huge = new string('x', ClassificationMatcher.KEYWORD_MAX_LENGTH + 1);

            var ex = Assert.Throws<DomainException>(() => new ClassificationMatcher(keyword: huge));

            Assert.Equal("AP.CMR03", ex.Id);
        }

        // Keyword é trimada e armazenada com case original — comparação no Matches é case-insensitive.
        [Fact]
        public void Construct_WithKeywordSurroundedByWhitespace_ShouldTrimAndKeep()
        {
            var matcher = new ClassificationMatcher(keyword: "   ALUGUEL   ");

            Assert.Equal("ALUGUEL", matcher.Keyword);
        }
    }

    public class WhenMatching
    {
        // Matcher só com SupplierId: casa se SupplierId do Payable coincide; ignora se diferente.
        [Fact]
        public void Matches_BySupplierId_ShouldReturnTrueWhenSupplierMatches()
        {
            var matcher = new ClassificationMatcher(supplierId: PayableMother.DEFAULT_SUPPLIER);
            var payable = PayableMother.Draft();

            Assert.True(matcher.Matches(payable));
        }

        [Fact]
        public void Matches_BySupplierId_ShouldReturnFalseWhenSupplierDiffers()
        {
            var matcher = new ClassificationMatcher(supplierId: OTHER_SUPPLIER);
            var payable = PayableMother.Draft();

            Assert.False(matcher.Matches(payable));
        }

        // Matcher só por keyword: comparação case-insensitive e partial-match na Description.
        [Theory]
        [InlineData("aluguel")]  // lowercase
        [InlineData("ALUGUEL")]  // uppercase
        [InlineData("Aluguel")]  // mixed
        [InlineData("luguel s")] // partial-match
        public void Matches_ByKeyword_ShouldBeCaseInsensitivePartialMatch(string keyword)
        {
            var matcher = new ClassificationMatcher(keyword: keyword);
            var payable = PayableMother.Draft(description: "Aluguel sede março");

            Assert.True(matcher.Matches(payable));
        }

        // Keyword que não está na Description não casa.
        [Fact]
        public void Matches_ByKeyword_ShouldReturnFalseWhenKeywordAbsent()
        {
            var matcher = new ClassificationMatcher(keyword: "energia");
            var payable = PayableMother.Draft(description: "Aluguel sede março");

            Assert.False(matcher.Matches(payable));
        }

        // Faixa de valor inclusiva nos limites (>= min e <= max).
        [Theory]
        [InlineData(1_000, true)]   // limite inferior incluído
        [InlineData(1_500, true)]   // dentro
        [InlineData(2_000, true)]   // limite superior incluído
        [InlineData(999, false)]    // abaixo
        [InlineData(2_001, false)]  // acima
        public void Matches_ByAmountRange_ShouldBeInclusive(decimal amount, bool expected)
        {
            var matcher = new ClassificationMatcher(
                minAmount: new Money(1_000m, Currency.Brl),
                maxAmount: new Money(2_000m, Currency.Brl));
            var payable = PayableMother.Draft(amount: amount);

            Assert.Equal(expected, matcher.Matches(payable));
        }

        // Critérios combinados (Supplier + Keyword): AND — só casa se TODOS satisfazem.
        [Fact]
        public void Matches_WithMultipleCriteria_ShouldBeConjunctive()
        {
            var matcher = new ClassificationMatcher(
                supplierId: PayableMother.DEFAULT_SUPPLIER,
                keyword: "aluguel");
            var matchingPayable = PayableMother.Draft(description: "Aluguel sede");
            var supplierOnlyPayable = PayableMother.Draft(description: "Conta de luz");

            Assert.True(matcher.Matches(matchingPayable));
            Assert.False(matcher.Matches(supplierOnlyPayable)); // supplier bate mas keyword não
        }
    }
}
