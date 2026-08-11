namespace BillPayment.UnitTests.Secrets;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Secrets;

public class SecretKindTests
{
    // Os ids são gravados na linha cifrada e entram no dado autenticado — mudá-los tornaria
    // ilegível todo segredo já guardado. Este teste é o que trava essa mudança.
    [Theory]
    [InlineData(1, "AsaasAccountApiKey")]
    [InlineData(2, "MailboxOAuthToken")]
    [InlineData(3, "PortalCredential")]
    [InlineData(4, "PdfPassword")]
    public void FromValue_ShouldKeepThePersistedIdsStable(int id, string expectedName)
    {
        Assert.Equal(expectedName, Enumeration.FromValue<SecretKind>(id).Name);
    }

    // O catálogo é o do ADR-009: acrescentar é barato, renumerar não.
    [Fact]
    public void GetAll_ShouldDeclareTheFourKindsFromTheAdr()
    {
        Assert.Equal(4, Enumeration.GetAll<SecretKind>().Count());
    }
}
