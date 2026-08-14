namespace TenantManagement.UnitTests.SharedKernel;

using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.SharedKernel;

public class ContactInfoTests
{
    // O e-mail é guardado normalizado e o telefone só com dígitos.
    [Fact]
    public void Create_WithMaskedPhoneAndMixedCaseEmail_ShouldNormalize()
    {
        var contact = ContactInfo.Create("  Contato@Rufino.COM.br ", "(11) 98765-4321");

        Assert.Equal("contato@rufino.com.br", contact.Email);
        Assert.Equal("11987654321", contact.Phone);
    }

    // Telefone é opcional: ausente vira string vazia, não erro.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutPhone_ShouldAcceptEmpty(string? phone)
    {
        Assert.Equal(string.Empty, ContactInfo.Create("contato@rufino.com.br", phone).Phone);
    }

    // E-mail em branco é reprovado em TNM.CTC01 — é por ele que chega o convite de acesso.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutEmail_ShouldThrow_TNM_CTC01(string email)
    {
        var error = Assert.Throws<DomainException>(() => ContactInfo.Create(email, null));

        Assert.Equal("TNM.CTC01", error.Id);
    }

    // E-mail sintaticamente inválido é reprovado em TNM.CTC02.
    [Theory]
    [InlineData("sem-arroba")]
    [InlineData("dois@@arrobas.com")]
    [InlineData("sem@dominio")]
    [InlineData("@semlocal.com")]
    [InlineData("espaco no@local.com")]
    public void Create_WithInvalidEmail_ShouldThrow_TNM_CTC02(string email)
    {
        var error = Assert.Throws<DomainException>(() => ContactInfo.Create(email, null));

        Assert.Equal("TNM.CTC02", error.Id);
    }

    // Telefone fora da faixa de 10 a 11 dígitos é reprovado em TNM.CTC03.
    [Theory]
    [InlineData("123456789")]
    [InlineData("123456789012")]
    public void Create_WithInvalidPhone_ShouldThrow_TNM_CTC03(string phone)
    {
        var error = Assert.Throws<DomainException>(() => ContactInfo.Create("contato@rufino.com.br", phone));

        Assert.Equal("TNM.CTC03", error.Id);
    }

    // Fixo com 10 dígitos e celular com 11 são igualmente aceitos.
    [Theory]
    [InlineData("1133224455")]
    [InlineData("11987654321")]
    public void Create_WithValidPhoneLengths_ShouldAccept(string phone)
    {
        Assert.Equal(phone, ContactInfo.Create("contato@rufino.com.br", phone).Phone);
    }

    // Dois contatos com os mesmos dados são o mesmo contato — igualdade por valor.
    [Fact]
    public void Equals_WithSameComponents_ShouldBeTrue()
    {
        Assert.Equal(
            ContactInfo.Create("contato@rufino.com.br", "1133224455"),
            ContactInfo.Create("CONTATO@rufino.com.br", "(11) 3322-4455"));
    }
}
