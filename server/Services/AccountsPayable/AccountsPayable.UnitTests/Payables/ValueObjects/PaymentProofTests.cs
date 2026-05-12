namespace AccountsPayable.UnitTests.Payables.ValueObjects;

using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;

public class PaymentProofTests
{
    // PaymentProof aceita URI absoluta válida e um tipo, preservando ambos.
    [Theory]
    [InlineData("https://docs.acme.com.br/proof.pdf")]
    [InlineData("http://internal.local/file.png")]
    [InlineData("s3://bucket/key.pdf")]
    public void Constructor_WithValidAbsoluteUri_ShouldPreserveValues(string uri)
    {
        var proof = new PaymentProof(uri, PaymentProofType.Receipt);

        Assert.Equal(uri, proof.Uri);
        Assert.Equal(PaymentProofType.Receipt, proof.Type);
    }

    // URI vazia ou whitespace lança AP.PPF01.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithEmptyUri_ShouldThrowDomainException(string uri)
    {
        var ex = Assert.Throws<DomainException>(() => new PaymentProof(uri, PaymentProofType.Receipt));

        Assert.Equal("AP.PPF01", ex.Id);
    }

    // URI relativa ou malformada lança AP.PPF02.
    [Theory]
    [InlineData("nao-eh-uri")]
    [InlineData("/relative/path")]
    public void Constructor_WithInvalidUri_ShouldThrowDomainException(string uri)
    {
        var ex = Assert.Throws<DomainException>(() => new PaymentProof(uri, PaymentProofType.Receipt));

        Assert.Equal("AP.PPF02", ex.Id);
    }

    // URI excedendo URI_MAX_LENGTH lança AP.PPF03.
    [Fact]
    public void Constructor_WithUriTooLong_ShouldThrowDomainException()
    {
        var tooLong = "https://acme.com/" + new string('a', PaymentProof.URI_MAX_LENGTH);

        var ex = Assert.Throws<DomainException>(() => new PaymentProof(tooLong, PaymentProofType.Receipt));

        Assert.Equal("AP.PPF03", ex.Id);
    }

    // Type null lança AP.PPF04.
    [Fact]
    public void Constructor_WithNullType_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new PaymentProof("https://acme.com/file.pdf", null!));

        Assert.Equal("AP.PPF04", ex.Id);
    }

    // Dois PaymentProof com mesma URI e mesmo tipo são iguais (igualdade composta).
    [Fact]
    public void Equals_WithSameUriAndType_ShouldReturnTrue()
    {
        var a = new PaymentProof("https://acme.com/file.pdf", PaymentProofType.Receipt);
        var b = new PaymentProof("https://acme.com/file.pdf", PaymentProofType.Receipt);

        Assert.Equal(a, b);
    }
}
