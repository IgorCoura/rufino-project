namespace TenantManagement.Domain.Tenants;

using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.SharedKernel;

/// <summary>
/// Quem é o cliente da plataforma — pessoa física ou jurídica, no mesmo modelo. É este
/// Aggregate que emite o <see cref="TenantId"/> que os produtos usam na rota e no token,
/// e é ele que sabe quem tem acesso a quê.
/// </summary>
/// <remarks>
/// O cadastro fiscal de cada produto (o <c>Company</c> do RH, o <c>PayerProfile</c> de contas
/// a pagar) continua sendo do produto. Aqui mora a identidade, não a operação.
/// </remarks>
public sealed class Tenant : AggregateRoot<TenantId>
{
    public const int LEGAL_NAME_MAX_LENGTH = 200;
    public const int TRADE_NAME_MAX_LENGTH = 200;
    public const int SUSPENSION_REASON_MAX_LENGTH = 300;

    private readonly List<TenantProduct> _products = [];
    private readonly List<TenantMembership> _memberships = [];

    public TenantKind Kind { get; private set; } = default!;
    public string LegalName { get; private set; } = string.Empty;

    /// <summary>Nome fantasia. Vazio para pessoa física, por invariante.</summary>
    public string TradeName { get; private set; } = string.Empty;

    public TaxId PrimaryTaxId { get; private set; } = default!;
    public ContactInfo Contact { get; private set; } = default!;
    public Address Address { get; private set; } = default!;
    public TenantStatus Status { get; private set; } = default!;
    public string SuspensionReason { get; private set; } = string.Empty;

    public IReadOnlyCollection<TenantProduct> Products => _products.AsReadOnly();
    public IReadOnlyCollection<TenantMembership> Memberships => _memberships.AsReadOnly();

    /// <summary>
    /// Os produtos habilitados <strong>agora</strong> — o estado que o provedor de identidade
    /// precisa refletir para que o token diga em quais produtos este tenant vale.
    /// </summary>
    /// <remarks>
    /// Derivado de propósito, e exposto aqui em vez de o filtro ser reescrito por quem sincroniza:
    /// a linha desativada continua existindo (o histórico é o que explica cobrança e acesso
    /// passados), então quem esquecer o <c>IsActive</c> concede acesso a produto cancelado.
    /// </remarks>
    public IReadOnlyCollection<ProductCode> ActiveProducts
        => _products.FindAll(p => p.IsActive).ConvertAll(p => p.Code).AsReadOnly();

    /// <summary>
    /// Estado da concessão de acesso no provedor de identidade, somando todos os vínculos.
    /// Derivado de propósito: a verdade está em cada vínculo, e um campo próprio no tenant
    /// seria uma segunda versão da mesma informação, livre para divergir.
    /// </summary>
    public ProvisioningStatus AccessProvisioning
    {
        get
        {
            if (_memberships.Exists(m => m.Provisioning.Equals(ProvisioningStatus.Failed)))
                return ProvisioningStatus.Failed;
            if (_memberships.Exists(m => m.Provisioning.Equals(ProvisioningStatus.Pending)))
                return ProvisioningStatus.Pending;
            return ProvisioningStatus.Done;
        }
    }

    private Tenant() { }

    private Tenant(TenantId id) : base(id) { }

    /// <summary>
    /// Cadastra o tenant e já concede acesso ao titular. Os dois passos são um só de
    /// propósito: cadastro sem dono é um tenant que ninguém consegue abrir, e é justamente
    /// o estado que ninguém percebe até precisar dele.
    /// </summary>
    public static Tenant Register(
        TenantId id,
        TenantKind kind,
        string legalName,
        string? tradeName,
        TaxId primaryTaxId,
        ContactInfo contact,
        Address address,
        string ownerEmail,
        DateTime occurredAt)
    {
        var tenant = new Tenant(id);

        tenant.SetKind(kind);
        tenant.SetLegalName(legalName);
        tenant.SetTradeName(tradeName);
        tenant.SetPrimaryTaxId(primaryTaxId);
        tenant.SetContact(contact);
        tenant.SetAddress(address);
        tenant.Status = TenantStatus.Active;
        tenant.CreatedAt = occurredAt;
        tenant.UpdatedAt = occurredAt;

        tenant.AddDomainEvent(new TenantRegisteredDomainEvent(id, tenant.LegalName, kind.Name, occurredAt));
        tenant.GrantMembership(ownerEmail, MembershipRole.Owner, occurredAt);

        return tenant;
    }

    public void Rename(string legalName, string? tradeName, DateTime occurredAt)
    {
        EnsureNotSuspended();

        SetLegalName(legalName);
        SetTradeName(tradeName);
        UpdatedAt = occurredAt;
    }

    public void ChangeContact(ContactInfo contact, DateTime occurredAt)
    {
        EnsureNotSuspended();

        SetContact(contact);
        UpdatedAt = occurredAt;
    }

    public void ChangeAddress(Address address, DateTime occurredAt)
    {
        EnsureNotSuspended();

        SetAddress(address);
        UpdatedAt = occurredAt;
    }

    public void Suspend(string reason, DateTime occurredAt)
    {
        if (Status.Equals(TenantStatus.Suspended))
            throw TenantErrors.AlreadySuspended();

        var normalized = (reason ?? string.Empty).Trim();
        if (normalized.Length == 0)
            throw TenantErrors.SuspensionReasonRequired();
        if (normalized.Length > SUSPENSION_REASON_MAX_LENGTH)
            normalized = normalized[..SUSPENSION_REASON_MAX_LENGTH];

        Status = TenantStatus.Suspended;
        SuspensionReason = normalized;
        UpdatedAt = occurredAt;

        AddDomainEvent(new TenantSuspendedDomainEvent(Id, normalized, occurredAt));
    }

    public void Reactivate(DateTime occurredAt)
    {
        if (!Status.Equals(TenantStatus.Suspended))
            throw TenantErrors.NotSuspended();

        Status = TenantStatus.Active;
        SuspensionReason = string.Empty;
        UpdatedAt = occurredAt;

        AddDomainEvent(new TenantReactivatedDomainEvent(Id, occurredAt));
    }

    /// <summary>Habilita um produto. Idempotente: habilitar o que já está habilitado não é erro.</summary>
    public void ActivateProduct(ProductCode product, DateTime occurredAt)
    {
        EnsureNotSuspended();

        if (product is null)
            throw TenantErrors.ProductRequired();

        var existing = _products.Find(p => p.Code.Equals(product));
        if (existing is not null)
        {
            if (existing.IsActive)
                return;

            existing.Reactivate(occurredAt);
        }
        else
        {
            _products.Add(TenantProduct.Activate(Id, product, occurredAt));
        }

        UpdatedAt = occurredAt;
        AddDomainEvent(new ProductActivatedDomainEvent(Id, product.Name, occurredAt));
    }

    /// <summary>Habilita uma lista de produtos. Idempotente: habilitar o que já está habilitado não é erro.</summary>
    public void ActivateProductRange(IEnumerable<ProductCode> products, DateTime occurredAt)
    {
        foreach(var product in products)
        {
            ActivateProduct(product, occurredAt);
        }
    }

    public void DeactivateProduct(ProductCode product, DateTime occurredAt)
    {
        EnsureNotSuspended();

        if (product is null)
            throw TenantErrors.ProductRequired();

        var existing = _products.Find(p => p.Code.Equals(product) && p.IsActive)
            ?? throw TenantErrors.ProductNotActive(product.Name);

        existing.Deactivate(occurredAt);
        UpdatedAt = occurredAt;

        AddDomainEvent(new ProductDeactivatedDomainEvent(Id, product.Name, occurredAt));
    }

    public bool HasActiveProduct(ProductCode product)
        => product is not null && _products.Exists(p => p.Code.Equals(product) && p.IsActive);

    /// <summary>
    /// Dá acesso ao tenant para um e-mail. Reconceder a quem já tem apenas ajusta o papel —
    /// é o que faz o cadastro tolerar o mesmo convite enviado duas vezes.
    /// </summary>
    public void GrantMembership(string email, MembershipRole role, DateTime occurredAt)
    {
        EnsureNotSuspended();

        if (role is null)
            throw TenantErrors.MembershipRoleRequired();

        var normalized = TenantMembership.NormalizeEmail(email);
        var existing = _memberships.Find(m => string.Equals(m.Email, normalized, StringComparison.Ordinal));

        if (existing is null)
        {
            _memberships.Add(TenantMembership.Grant(Id, normalized, role, occurredAt));
        }
        else if (existing.IsActive)
        {
            if (existing.Role.Equals(role))
                return;

            EnsureOwnerSurvives(existing, role);
            existing.ChangeRole(role, occurredAt);
            UpdatedAt = occurredAt;
            return;
        }
        else
        {
            existing.Regrant(role, occurredAt);
        }

        UpdatedAt = occurredAt;
        AddDomainEvent(new MembershipGrantedDomainEvent(Id, normalized, role.Name, LegalName, occurredAt));
    }

    public void RevokeMembership(string email, DateTime occurredAt)
    {
        var normalized = TenantMembership.NormalizeEmail(email);
        var membership = _memberships.Find(m => string.Equals(m.Email, normalized, StringComparison.Ordinal) && m.IsActive)
            ?? throw TenantErrors.MembershipNotFound(normalized);

        EnsureOwnerSurvives(membership, MembershipRole.Member);

        membership.Revoke(occurredAt);
        UpdatedAt = occurredAt;

        AddDomainEvent(new MembershipRevokedDomainEvent(Id, normalized, occurredAt));
    }

    /// <summary>O provedor de identidade confirmou o acesso; o vínculo deixa de estar pendente.</summary>
    public void ConfirmAccessProvisioned(string email, UserId? userId, DateTime occurredAt)
    {
        var membership = FindMembershipOrThrow(email);
        membership.ConfirmProvisioned(userId, occurredAt);
        UpdatedAt = occurredAt;
    }

    public void MarkAccessProvisioningFailed(string email, DateTime occurredAt)
    {
        var membership = FindMembershipOrThrow(email);
        membership.MarkProvisioningFailed(occurredAt);
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Recoloca na fila todo vínculo que não chegou ao provedor. É o caminho de conserto de
    /// um provisionamento que falhou — e é idempotente porque o que já está feito fica quieto.
    /// </summary>
    /// <remarks>
    /// <strong>Num tenant suspenso, reprocessar REVOGA — nunca reconcede.</strong> O que a fila
    /// persegue é o estado desejado no provedor, e num tenant suspenso esse estado é "ninguém
    /// tem acesso a este tenant", independentemente de o vínculo seguir ativo no cadastro (ele
    /// segue, de propósito: suspender preserva o cadastro). Emitir concessão aqui transformaria
    /// o endpoint de reprovisionamento na forma de burlar a suspensão — bastava pedir o conserto
    /// de um vínculo pendente para todo mundo voltar a entrar.
    /// </remarks>
    public IReadOnlyCollection<TenantMembership> RequeueFailedAccessProvisioning(DateTime occurredAt)
    {
        var pending = _memberships.FindAll(m => m.NeedsProvisioning());
        var suspended = Status.Equals(TenantStatus.Suspended);

        foreach (var membership in pending)
        {
            membership.MarkProvisioningPending(occurredAt);

            if (membership.IsActive && !suspended)
                AddDomainEvent(new MembershipGrantedDomainEvent(Id, membership.Email, membership.Role.Name, LegalName, occurredAt));
            else
                AddDomainEvent(new MembershipRevokedDomainEvent(Id, membership.Email, occurredAt));
        }

        if (pending.Count > 0)
            UpdatedAt = occurredAt;

        return pending.AsReadOnly();
    }

    public bool HasActiveMember(string email)
    {
        var normalized = EmailSyntax.Normalize(email);
        return _memberships.Exists(m => m.IsActive && string.Equals(m.Email, normalized, StringComparison.Ordinal));
    }

    private TenantMembership FindMembershipOrThrow(string email)
    {
        var normalized = TenantMembership.NormalizeEmail(email);
        return _memberships.Find(m => string.Equals(m.Email, normalized, StringComparison.Ordinal))
            ?? throw TenantErrors.MembershipNotFound(normalized);
    }

    // Tirar o papel de dono do último dono deixaria o tenant sem ninguém que responda por ele,
    // e o socorro seria mexer no banco à mão.
    private void EnsureOwnerSurvives(TenantMembership membership, MembershipRole newRole)
    {
        if (!membership.Role.Equals(MembershipRole.Owner) || newRole.Equals(MembershipRole.Owner))
            return;

        var otherOwners = _memberships.Exists(m =>
            m.IsActive
            && m.Role.Equals(MembershipRole.Owner)
            && !string.Equals(m.Email, membership.Email, StringComparison.Ordinal));

        if (!otherOwners)
            throw TenantErrors.LastOwnerCannotBeRevoked();
    }

    private void EnsureNotSuspended()
    {
        if (Status is not null && Status.Equals(TenantStatus.Suspended))
            throw TenantErrors.SuspendedTenantIsReadOnly();
    }

    private void SetKind(TenantKind kind)
        => Kind = kind ?? throw TenantErrors.KindRequired();

    private void SetLegalName(string legalName)
    {
        var normalized = (legalName ?? string.Empty).Trim();
        if (normalized.Length == 0)
            throw TenantErrors.LegalNameRequired();
        if (normalized.Length > LEGAL_NAME_MAX_LENGTH)
            throw TenantErrors.LegalNameTooLong(LEGAL_NAME_MAX_LENGTH);

        LegalName = normalized;
    }

    private void SetTradeName(string? tradeName)
    {
        var normalized = (tradeName ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            TradeName = string.Empty;
            return;
        }

        if (!Kind.AllowsTradeName)
            throw TenantErrors.TradeNameRequiresCompany();
        if (normalized.Length > TRADE_NAME_MAX_LENGTH)
            throw TenantErrors.TradeNameTooLong(TRADE_NAME_MAX_LENGTH);

        TradeName = normalized;
    }

    private void SetPrimaryTaxId(TaxId primaryTaxId)
    {
        if (primaryTaxId is null)
            throw TenantErrors.PrimaryTaxIdRequired();
        if (!primaryTaxId.Kind.Equals(Kind.ExpectedPrimaryTaxIdKind))
            throw TenantErrors.PrimaryTaxIdKindMismatch(
                Kind.Name, Kind.ExpectedPrimaryTaxIdKind.Name, primaryTaxId.Kind.Name);

        PrimaryTaxId = primaryTaxId;
    }

    private void SetContact(ContactInfo contact)
        => Contact = contact ?? throw TenantErrors.ContactRequired();

    private void SetAddress(Address address)
        => Address = address ?? throw TenantErrors.AddressRequired();
}
