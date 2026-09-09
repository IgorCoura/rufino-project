namespace BillPayment.UnitTests.PayerProfiles;

using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SeedWork;
using BillPayment.UnitTests.PayerProfiles.Mothers;

/// <summary>
/// O webhook do tenant no agregado: quando o token conta como rotacionado, quando o conserto NÃO
/// mexe nesse relógio, e quando o silêncio do provedor vira pergunta.
/// </summary>
public sealed class PayerProfileWebhookTests
{
    private static readonly DateTime Provisioned = PayerProfileMother.DefaultOccurredAt.AddDays(3);

    private static readonly CredentialRef WebhookRef =
        CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-0000000000be"));

    // Vincular o webhook carimba a rotação: é ele que marca "este token nasceu agora".
    [Fact]
    public void LinkAsaasWebhook_ShouldStampTheRotationClock()
    {
        var profile = LinkedProfile();

        profile.LinkAsaasWebhook(WebhookRef, "wh_001", Provisioned);

        Assert.True(profile.HasWebhook);
        Assert.Equal("wh_001", profile.AsaasWebhookId);
        Assert.Equal(Provisioned, profile.AsaasWebhookRotatedAt);
        Assert.Equal(Provisioned, profile.UpdatedAt);
    }

    // Id em branco é recusado — BLP.PRF14: sem ele não há como atualizar nem remover no provedor.
    [Fact]
    public void LinkAsaasWebhook_WithBlankProviderId_ShouldThrow_BLP_PRF14()
    {
        var profile = LinkedProfile();

        var ex = Assert.Throws<DomainException>(
            () => profile.LinkAsaasWebhook(WebhookRef, "   ", Provisioned));

        Assert.Equal("BLP.PRF14", ex.Id);
    }

    // A distinção que sustenta a varredura: consertar NÃO é rotacionar. O relógio fica onde está.
    [Fact]
    public void ConfirmAsaasWebhook_ShouldNotMoveTheRotationClock()
    {
        var profile = LinkedProfile();
        profile.LinkAsaasWebhook(WebhookRef, "wh_001", Provisioned);

        var healedAt = Provisioned.AddHours(5);
        profile.ConfirmAsaasWebhook("wh_001", healedAt);

        Assert.Equal(Provisioned, profile.AsaasWebhookRotatedAt);
        Assert.Equal(healedAt, profile.UpdatedAt);
    }

    // Confirmar sem token no cofre é estado impossível — BLP.PRF15: não há o que reaproveitar.
    [Fact]
    public void ConfirmAsaasWebhook_WithoutAStoredToken_ShouldThrow_BLP_PRF15()
    {
        var profile = LinkedProfile();

        var ex = Assert.Throws<DomainException>(
            () => profile.ConfirmAsaasWebhook("wh_001", Provisioned));

        Assert.Equal("BLP.PRF15", ex.Id);
    }

    // Desvincular apaga tudo, inclusive o relógio — senão o webhook seguinte nasceria "velho".
    [Fact]
    public void UnlinkAsaasWebhook_ShouldClearTheRotationClockToo()
    {
        var profile = LinkedProfile();
        profile.LinkAsaasWebhook(WebhookRef, "wh_001", Provisioned);
        profile.RecordWebhookActivity(Provisioned.AddHours(1));

        profile.UnlinkAsaasWebhook(Provisioned.AddHours(2));

        Assert.False(profile.HasWebhook);
        Assert.Null(profile.AsaasWebhookRotatedAt);
        Assert.Null(profile.LastWebhookEventAt);
    }

    // Sem webhook não há rotação a vencer — a pergunta não se aplica.
    [Fact]
    public void IsWebhookRotationDue_WithoutAWebhook_ShouldBeFalse()
    {
        var profile = LinkedProfile();

        Assert.False(profile.IsWebhookRotationDue(Provisioned.AddYears(1), TimeSpan.FromDays(90)));
    }

    [Theory]
    [InlineData(89, false)]
    [InlineData(90, true)]
    [InlineData(120, true)]
    public void IsWebhookRotationDue_ShouldFollowTheConfiguredInterval(int daysElapsed, bool expected)
    {
        var profile = LinkedProfile();
        profile.LinkAsaasWebhook(WebhookRef, "wh_001", Provisioned);

        var due = profile.IsWebhookRotationDue(
            Provisioned.AddDays(daysElapsed), TimeSpan.FromDays(90));

        Assert.Equal(expected, due);
    }

    // Webhook recém-provisionado que nunca recebeu nada NÃO está mudo — está novo. A referência
    // é o provisionamento enquanto não há evento.
    [Fact]
    public void IsWebhookSilentSince_RightAfterProvisioning_ShouldBeFalse()
    {
        var profile = LinkedProfile();
        profile.LinkAsaasWebhook(WebhookRef, "wh_001", Provisioned);

        Assert.False(profile.IsWebhookSilentSince(Provisioned.AddHours(1), TimeSpan.FromHours(24)));
        Assert.True(profile.IsWebhookSilentSince(Provisioned.AddHours(25), TimeSpan.FromHours(24)));
    }

    // Chegando evento, a referência passa a ser ele — o relógio do silêncio reinicia.
    [Fact]
    public void IsWebhookSilentSince_ShouldMeasureFromTheLastEvent()
    {
        var profile = LinkedProfile();
        profile.LinkAsaasWebhook(WebhookRef, "wh_001", Provisioned);
        profile.RecordWebhookActivity(Provisioned.AddHours(20));

        Assert.False(profile.IsWebhookSilentSince(Provisioned.AddHours(30), TimeSpan.FromHours(24)));
        Assert.True(profile.IsWebhookSilentSince(Provisioned.AddHours(45), TimeSpan.FromHours(24)));
    }

    private static PayerProfile LinkedProfile()
    {
        var profile = PayerProfileMother.Register();
        profile.LinkAsaasAccount(
            CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-00000000c0fe")),
            PayerProfileMother.DefaultOccurredAt);

        return profile;
    }
}
