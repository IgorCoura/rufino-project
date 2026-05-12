namespace AccountsPayable.UnitTests.Suppliers.ValueObjects;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.ValueObjects;

public class ContactInfoTests
{
    // ContactInfo é construído com email obrigatório; phone e address são opcionais.
    [Fact]
    public void Constructor_WithOnlyEmail_ShouldSucceedAndLeaveOptionalsNull()
    {
        var email = new EmailAddress("foo@bar.com");

        var contact = new ContactInfo(email);

        Assert.Equal(email, contact.Email);
        Assert.Null(contact.Phone);
        Assert.Null(contact.Address);
    }

    // ContactInfo com todos os campos preenchidos preserva cada um.
    [Fact]
    public void Constructor_WithAllFields_ShouldPreserveAll()
    {
        var email = new EmailAddress("foo@bar.com");
        var phone = new PhoneNumber("11987654321");
        var address = new Address("Av. Paulista", "1000", null, "Bela Vista", "São Paulo", "SP", "01310100");

        var contact = new ContactInfo(email, phone, address);

        Assert.Equal(email, contact.Email);
        Assert.Equal(phone, contact.Phone);
        Assert.Equal(address, contact.Address);
    }

    // Email null lança AP.CTI01 (email é obrigatório).
    [Fact]
    public void Constructor_WithNullEmail_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new ContactInfo(null!));

        Assert.Equal("AP.CTI01", ex.Id);
    }

    // Dois ContactInfos com mesmos componentes (email + phone + address) são iguais.
    [Fact]
    public void Equals_WithSameComponents_ShouldReturnTrue()
    {
        var a = new ContactInfo(new EmailAddress("foo@bar.com"), new PhoneNumber("11987654321"));
        var b = new ContactInfo(new EmailAddress("foo@bar.com"), new PhoneNumber("11987654321"));

        Assert.Equal(a, b);
    }

    // ContactInfo só com email é diferente do mesmo email + phone (componente extra muda igualdade).
    [Fact]
    public void Equals_DifferingByPhonePresence_ShouldReturnFalse()
    {
        var withoutPhone = new ContactInfo(new EmailAddress("foo@bar.com"));
        var withPhone = new ContactInfo(new EmailAddress("foo@bar.com"), new PhoneNumber("11987654321"));

        Assert.NotEqual(withoutPhone, withPhone);
    }
}
