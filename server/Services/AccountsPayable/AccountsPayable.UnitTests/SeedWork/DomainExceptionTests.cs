namespace AccountsPayable.UnitTests.SeedWork;

using AccountsPayable.Domain.SeedWork;

public class DomainExceptionTests
{
    // Ids que casam com o padrão (AP01, AP.PAY01, APD.BIL12, SWK01) são aceitos pelo construtor.
    [Theory]
    [InlineData("AP01")]
    [InlineData("AP.PAY01")]
    [InlineData("APD.BIL12")]
    [InlineData("SWK01")]
    public void Constructor_AcceptsValidIdPattern(string id)
    {
        var ex = new DomainException(id, "ok", Array.Empty<object>(), "File.cs:10 (Method)");
        Assert.Equal(id, ex.Id);
    }

    // Ids fora do padrão (letras a menos, minúsculas, sem dígito, separador errado) lançam ArgumentException.
    [Theory]
    [InlineData("A01")]
    [InlineData("AP.A01")]
    [InlineData("ap.pay01")]
    [InlineData("AP.PAY")]
    [InlineData("AP-PAY01")]
    public void Constructor_RejectsInvalidIdPattern(string id)
    {
        Assert.Throws<ArgumentException>(() =>
            new DomainException(id, "ok", Array.Empty<object>(), "File.cs:10 (Method)"));
    }

    // Message é o MessageTemplate formatado via string.Format com os Parameters fornecidos.
    [Fact]
    public void Constructor_FormatsMessageWithParameters()
    {
        var ex = new DomainException(
            id: "AP.PAY01",
            messageTemplate: "Transição inválida: {0} → {1}.",
            parameters: new object[] { "DRAFT", "PAID" },
            sourcePath: "Payable.cs:42 (Approve)");

        Assert.Equal("Transição inválida: DRAFT → PAID.", ex.Message);
        Assert.Equal("Transição inválida: {0} → {1}.", ex.MessageTemplate);
        Assert.Equal(2, ex.Parameters.Count);
    }

    // Parameters contendo null lançam ArgumentException — proteção contra factories mal escritas.
    [Fact]
    public void Constructor_RejectsNullParameterEntries()
    {
        Assert.Throws<ArgumentException>(() =>
            new DomainException("AP01", "x", new object[] { null! }, "f.cs:1 (m)"));
    }

    // ToString() formata como "[Id] Message | at SourcePath" — formato esperado pelos logs.
    [Fact]
    public void ToString_IncludesIdMessageAndSourcePath()
    {
        var ex = new DomainException("AP.PAY02", "Falhou.", Array.Empty<object>(), "Payable.cs:7 (Reject)");
        Assert.Equal("[AP.PAY02] Falhou. | at Payable.cs:7 (Reject)", ex.ToString());
    }
}
