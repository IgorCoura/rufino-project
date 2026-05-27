namespace EconomicCore.Domain.SeedWork;

public sealed class DomainErrorCategory : Enumeration
{
    public static readonly DomainErrorCategory Validation = new(400, nameof(Validation));
    public static readonly DomainErrorCategory NotFound = new(404, nameof(NotFound));
    public static readonly DomainErrorCategory Conflict = new(409, nameof(Conflict));

    private DomainErrorCategory(int id, string name) : base(id, name) { }
}
