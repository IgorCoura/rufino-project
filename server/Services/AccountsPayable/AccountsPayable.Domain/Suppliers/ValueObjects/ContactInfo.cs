namespace AccountsPayable.Domain.Suppliers.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Contact info bundle: email is required; phone and address are optional.
/// Composite VO — equality compares the inner components.
/// </summary>
public sealed class ContactInfo : ValueObject
{
    public EmailAddress Email { get; }
    public PhoneNumber? Phone { get; }
    public Address? Address { get; }

    public ContactInfo(EmailAddress email, PhoneNumber? phone = null, Address? address = null)
    {
        if (email is null)
            throw ContactInfoErrors.EmailRequired();
        Email = email;
        Phone = phone;
        Address = address;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Email;
        yield return Phone;
        yield return Address;
    }
}
