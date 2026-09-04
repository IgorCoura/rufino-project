namespace BillPayment.UnitTests.CapturedMessages;

using BillPayment.Domain.SeedWork;
using BillPayment.UnitTests.CapturedMessages.Mothers;

/// <summary>
/// O corpo do e-mail retido no livro-caixa — a base do "ver e-mail" e da extração por IA.
/// </summary>
public class CapturedMessageBodyTests
{
    private static readonly DateTime RecordedAt = new(2026, 8, 27, 10, 0, 0, DateTimeKind.Utc);

    // Registrar o corpo guarda a chave e o tipo, e o registro passa a ter corpo servível.
    [Fact]
    public void RecordBody_WithKeyAndContentType_ShouldExposeTheStoredBody()
    {
        var message = CapturedMessageMother.Register();

        message.RecordBody("tenant/message-body-abc", "text/html", RecordedAt);

        Assert.True(message.HasStoredBody);
        Assert.Equal("tenant/message-body-abc", message.BodyStorageKey);
        Assert.Equal("text/html", message.BodyContentType);
        Assert.Equal(RecordedAt, message.UpdatedAt);
    }

    // Mensagem recém-registrada não tem corpo — é o estado de todo registro anterior à retenção.
    [Fact]
    public void Register_ShouldStartWithoutStoredBody()
    {
        var message = CapturedMessageMother.Register();

        Assert.False(message.HasStoredBody);
        Assert.Null(message.BodyStorageKey);
    }

    // Chave vazia é recusada (BLP.CMS09): corpo sem endereço não é corpo guardado.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordBody_WithoutStorageKey_Throws_BLP_CMS09(string? storageKey)
    {
        var message = CapturedMessageMother.Register();

        var exception = Assert.Throws<DomainException>(
            () => message.RecordBody(storageKey!, "text/html", RecordedAt));

        Assert.Equal("BLP.CMS09", exception.Id);
    }

    // Tipo de conteúdo vazio é recusado (BLP.CMS10): sem ele a tela não sabe como renderizar.
    [Fact]
    public void RecordBody_WithoutContentType_Throws_BLP_CMS10()
    {
        var message = CapturedMessageMother.Register();

        var exception = Assert.Throws<DomainException>(
            () => message.RecordBody("tenant/message-body-abc", " ", RecordedAt));

        Assert.Equal("BLP.CMS10", exception.Id);
    }
}
