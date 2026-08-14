namespace TenantManagement.Domain.SharedKernel;

using TenantManagement.Domain.SeedWork;

/// <summary>
/// Identidade da pessoa no provedor de identidade. O domínio não sabe qual provedor é —
/// sabe apenas que é um identificador estável que chega de fora e nunca é gerado aqui.
/// </summary>
public readonly record struct UserId(Guid Value) : IEntityId<UserId>
{
    public static UserId New() => new(Guid.CreateVersion7());
    public static UserId From(Guid value) => new(value);
    public static UserId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
