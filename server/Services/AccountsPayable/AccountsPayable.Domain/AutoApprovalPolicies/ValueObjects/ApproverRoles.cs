namespace AccountsPayable.Domain.AutoApprovalPolicies.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Non-empty, normalized, duplicate-free set of role names eligible to approve a payable that
/// matches an <c>ApprovalRule</c>. Each role is trimmed and upper-cased. The role string is
/// opaque to the Domain — semantics (e.g., "OWNER", "FINANCE_LEAD") live in the Keycloak/IAM
/// configuration consumed by the Application layer.
/// </summary>
public sealed class ApproverRoles : ValueObject
{
    public const int MAX_ROLE_LENGTH = 64;

    public IReadOnlyList<string> Roles { get; }

    public ApproverRoles(IEnumerable<string> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);

        var normalized = new List<string>();
        foreach (var raw in roles)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw ApproverRolesErrors.RoleEmpty();
            var trimmed = raw.Trim().ToUpperInvariant();
            if (trimmed.Length > MAX_ROLE_LENGTH)
                throw ApproverRolesErrors.RoleTooLong(MAX_ROLE_LENGTH);
            if (normalized.Contains(trimmed))
                throw ApproverRolesErrors.DuplicateRole(trimmed);
            normalized.Add(trimmed);
        }

        if (normalized.Count == 0)
            throw ApproverRolesErrors.AtLeastOneRoleRequired();

        Roles = normalized.AsReadOnly();
    }

    public bool Contains(string role) =>
        !string.IsNullOrWhiteSpace(role)
        && Roles.Contains(role.Trim().ToUpperInvariant());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var r in Roles)
            yield return r;
    }
}
