namespace BillPayment.Domain.SeedWork;

public sealed class DomainErrorCategory : Enumeration
{
    public static readonly DomainErrorCategory Validation = new(400, nameof(Validation));

    /// <summary>Regra de alçada: quem chamou não pode fazer ISTO neste objeto (403, não 409).</summary>
    public static readonly DomainErrorCategory Forbidden = new(403, nameof(Forbidden));

    public static readonly DomainErrorCategory NotFound = new(404, nameof(NotFound));
    public static readonly DomainErrorCategory Conflict = new(409, nameof(Conflict));

    private DomainErrorCategory(int id, string name) : base(id, name) { }
}
