namespace AccountsPayable.UnitTests.Suppliers.ValueObjects;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.Enumerations;
using AccountsPayable.Domain.Suppliers.ValueObjects;

public class TaxIdTests
{
    public class WhenCreatingFromCpf
    {
        // Construir TaxId com CPF válido formatado (com pontos e hífen) normaliza para 11 dígitos e classifica como CPF.
        [Theory]
        [InlineData("123.456.789-09")]
        [InlineData("12345678909")]
        [InlineData("123 456 789 09")]
        public void Constructor_WithValidCpf_ShouldNormalizeToDigitsAndClassifyAsCpf(string raw)
        {
            var taxId = new TaxId(raw);

            Assert.Equal("12345678909", taxId.Value);
            Assert.Equal(TaxIdType.Cpf, taxId.Type);
        }

        // Construir TaxId com CPF inválido (check digits errados) lança AP.TXI02.
        [Fact]
        public void Constructor_WithInvalidCpfCheckDigits_ShouldThrowDomainException()
        {
            var ex = Assert.Throws<DomainException>(() => new TaxId("123.456.789-10"));

            Assert.Equal("AP.TXI02", ex.Id);
        }

        // CPFs com todos os dígitos iguais (111... 999...) são rejeitados mesmo quando casariam aritmeticamente.
        [Theory]
        [InlineData("00000000000")]
        [InlineData("11111111111")]
        [InlineData("99999999999")]
        public void Constructor_WithRepeatedDigitCpf_ShouldThrowDomainException(string raw)
        {
            var ex = Assert.Throws<DomainException>(() => new TaxId(raw));

            Assert.Equal("AP.TXI02", ex.Id);
        }
    }

    public class WhenCreatingFromCnpj
    {
        // Construir TaxId com CNPJ válido formatado normaliza para 14 dígitos e classifica como CNPJ.
        [Theory]
        [InlineData("11.444.777/0001-61")]
        [InlineData("11444777000161")]
        public void Constructor_WithValidCnpj_ShouldNormalizeToDigitsAndClassifyAsCnpj(string raw)
        {
            var taxId = new TaxId(raw);

            Assert.Equal("11444777000161", taxId.Value);
            Assert.Equal(TaxIdType.Cnpj, taxId.Type);
        }

        // Construir TaxId com CNPJ inválido (check digits errados) lança AP.TXI02.
        [Fact]
        public void Constructor_WithInvalidCnpjCheckDigits_ShouldThrowDomainException()
        {
            var ex = Assert.Throws<DomainException>(() => new TaxId("11.444.777/0001-62"));

            Assert.Equal("AP.TXI02", ex.Id);
        }

        // CNPJs com todos os dígitos iguais são rejeitados (proteção contra "00000000000000").
        [Fact]
        public void Constructor_WithRepeatedDigitCnpj_ShouldThrowDomainException()
        {
            var ex = Assert.Throws<DomainException>(() => new TaxId("00000000000000"));

            Assert.Equal("AP.TXI02", ex.Id);
        }
    }

    public class WhenCreatingWithBadInput
    {
        // Construir TaxId com string vazia/branca lança AP.TXI01.
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("    ")]
        public void Constructor_WithEmptyInput_ShouldThrowDomainException(string raw)
        {
            var ex = Assert.Throws<DomainException>(() => new TaxId(raw));

            Assert.Equal("AP.TXI01", ex.Id);
        }

        // Entradas com número de dígitos diferente de 11 (CPF) ou 14 (CNPJ) são rejeitadas como AP.TXI02.
        [Theory]
        [InlineData("123")]
        [InlineData("1234567890")]       // 10 dígitos
        [InlineData("123456789012")]     // 12 dígitos
        [InlineData("123456789012345")]  // 15 dígitos
        [InlineData("abcdefghijk")]      // sem dígitos válidos
        public void Constructor_WithWrongDigitCount_ShouldThrowDomainException(string raw)
        {
            var ex = Assert.Throws<DomainException>(() => new TaxId(raw));

            Assert.Equal("AP.TXI02", ex.Id);
        }
    }

    public class WhenFormatting
    {
        // CPF válido formatado retorna "###.###.###-##".
        [Fact]
        public void Formatted_ForCpf_ShouldReturnDottedHyphenFormat()
        {
            var taxId = new TaxId("12345678909");

            Assert.Equal("123.456.789-09", taxId.Formatted);
        }

        // CNPJ válido formatado retorna "##.###.###/####-##".
        [Fact]
        public void Formatted_ForCnpj_ShouldReturnDottedSlashHyphenFormat()
        {
            var taxId = new TaxId("11444777000161");

            Assert.Equal("11.444.777/0001-61", taxId.Formatted);
        }

        // MaskedValue de CPF mostra apenas os 3 dígitos do meio e os 2 finais — esconde os primeiros 6 (PII).
        [Fact]
        public void MaskedValue_ForCpf_ShouldHideFirstSixDigits()
        {
            var taxId = new TaxId("12345678909");

            Assert.Equal("***.***.789-09", taxId.MaskedValue);
        }

        // MaskedValue de CNPJ mostra somente filial (4) + verificadores (2) — esconde raiz do CNPJ.
        [Fact]
        public void MaskedValue_ForCnpj_ShouldHideRoot()
        {
            var taxId = new TaxId("11444777000161");

            Assert.Equal("**.***.***/0001-61", taxId.MaskedValue);
        }
    }

    public class WhenComparing
    {
        // Dois TaxIds construídos a partir de entradas equivalentes (formatadas vs cruas) são iguais.
        [Fact]
        public void Equals_WithEquivalentInputs_ShouldReturnTrue()
        {
            var a = new TaxId("123.456.789-09");
            var b = new TaxId("12345678909");

            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        // CPF e CNPJ válidos com mesma string normalizada não existem (lengths diferem) — testando casos representativos diferentes.
        [Fact]
        public void Equals_DifferentTaxIds_ShouldReturnFalse()
        {
            var cpf = new TaxId("12345678909");
            var cnpj = new TaxId("11444777000161");

            Assert.NotEqual<TaxId>(cpf, cnpj);
        }
    }
}
