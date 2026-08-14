namespace TenantManagement.Application.Queries.Tenants;

using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.SharedKernel;
using TenantManagement.Domain.Tenants;
using TenantManagement.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class TenantQueries(TenantManagementDbContext context) : ITenantQueries
{
    public const int DEFAULT_LIMIT = 50;
    public const int MAX_LIMIT = 200;

    public async Task<TenantPage> ListAsync(
        TenantListFilter filter,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var effectiveLimit = Math.Clamp(limit <= 0 ? DEFAULT_LIMIT : limit, 1, MAX_LIMIT);

        var query = context.Tenants
            .AsNoTracking()
            .Include(t => t.Products)
            .Include(t => t.Memberships)
            .AsQueryable();

        // Filtro desconhecido não devolve a lista inteira em silêncio: o valor inválido vira
        // erro de domínio, do mesmo jeito que viraria num comando.
        if (!string.IsNullOrWhiteSpace(filter.Kind))
        {
            var kind = Enumeration.TryFromDisplayName<TenantKind>(filter.Kind)
                ?? throw TenantErrors.UnknownKind(filter.Kind);
            query = query.Where(t => t.Kind == kind);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var status = Enumeration.TryFromDisplayName<TenantStatus>(filter.Status)
                ?? throw TenantErrors.UnknownStatus(filter.Status);
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(filter.Product))
        {
            var product = Enumeration.TryFromDisplayName<ProductCode>(filter.Product)
                ?? throw TenantErrors.UnknownProduct(filter.Product);
            query = query.Where(t => t.Products.Any(p => p.Code == product && p.IsActive));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            var digits = new string(term.Where(char.IsDigit).ToArray());
            var matchingByTaxId = await FindIdsByPartialTaxIdAsync(digits, cancellationToken);

            query = query.Where(t =>
                EF.Functions.ILike(t.LegalName, $"%{term}%")
                || EF.Functions.ILike(t.TradeName, $"%{term}%")
                || matchingByTaxId.Contains(t.Id));
        }

        // Keyset descendente por (CreatedAt, Id) — cadastro mais recente primeiro. O desempate
        // acompanha a direção da chave: cruzá-las faz ORDER BY e WHERE discordarem sobre o que
        // já foi visto, e a página seguinte volta vazia sem erro nenhum.
        if (CursorCodec.TryDecode(cursor, out var beforeCreatedAt, out var beforeId))
        {
            var beforeTenantId = TenantId.From(beforeId);

            query = query.Where(t =>
                t.CreatedAt < beforeCreatedAt || (t.CreatedAt == beforeCreatedAt && t.Id < beforeTenantId));
        }

        var rows = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Take(effectiveLimit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > effectiveLimit;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        var nextCursor = hasMore && rows.Count > 0
            ? CursorCodec.Encode(rows[^1].CreatedAt, rows[^1].Id.Value)
            : null;

        return new TenantPage(rows.ConvertAll(ToListItem), nextCursor);
    }

    public async Task<TenantDto?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var id = TenantId.From(tenantId);

        var tenant = await context.Tenants
            .AsNoTracking()
            .Include(t => t.Products)
            .Include(t => t.Memberships)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return tenant is null ? null : ToDto(tenant);
    }

    public async Task<IReadOnlyList<MyTenantDto>> ListForMemberAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = EmailSyntax.Normalize(email);
        if (normalized.Length == 0)
            return [];

        var tenants = await context.Tenants
            .AsNoTracking()
            .Include(t => t.Products)
            .Include(t => t.Memberships)
            .Where(t => t.Memberships.Any(m => m.IsActive && m.Email == normalized))
            .OrderBy(t => t.LegalName)
            .ToListAsync(cancellationToken);

        return tenants.ConvertAll(t => new MyTenantDto(
            t.Id.Value,
            t.Kind.Name,
            t.LegalName,
            t.TradeName,
            t.Status.Name,
            t.Memberships.First(m => m.IsActive && m.Email == normalized).Role.Name,
            ActiveProducts(t)));
    }

    /// <summary>
    /// Acha os tenants cujo documento contém os dígitos buscados — o atendente digita a raiz do
    /// CNPJ, não o documento inteiro.
    /// </summary>
    /// <remarks>
    /// Consulta separada, de propósito, e não um <c>LIKE</c> dentro do filtro principal.
    /// <c>PrimaryTaxId</c> é um Value Object com Value Converter, e o EF aplica o conversor
    /// <strong>também ao parâmetro</strong> do <c>LIKE</c>: o padrão <c>%11222333%</c> era
    /// empurrado por <c>TaxId.Parse</c> e a listagem inteira morria em
    /// <c>InvalidCastException</c> — 500, não "nenhum resultado". O custo é uma ida a mais ao
    /// banco, e só quando o termo tem dígito.
    /// </remarks>
    private async Task<List<TenantId>> FindIdsByPartialTaxIdAsync(string digits, CancellationToken cancellationToken)
    {
        if (digits.Length == 0)
            return [];

        var pattern = $"%{digits}%";

        var ids = await context.Database
            .SqlQuery<Guid>($"""
                SELECT id AS "Value"
                FROM tenant_management.tenants
                WHERE primary_tax_id LIKE {pattern}
                """)
            .ToListAsync(cancellationToken);

        return ids.ConvertAll(TenantId.From);
    }

    private static List<string> ActiveProducts(Tenant tenant)
        => tenant.Products.Where(p => p.IsActive).Select(p => p.Code.Name).ToList();

    private static TenantListItemDto ToListItem(Tenant t)
        => new(
            t.Id.Value,
            t.Kind.Name,
            t.LegalName,
            t.TradeName,
            t.PrimaryTaxId.Formatted(),
            t.Status.Name,
            t.AccessProvisioning.Name,
            t.Contact.Email,
            ActiveProducts(t),
            t.CreatedAt);

    private static TenantDto ToDto(Tenant t)
        => new(
            t.Id.Value,
            t.Kind.Name,
            t.LegalName,
            t.TradeName,
            t.PrimaryTaxId.Value,
            t.PrimaryTaxId.Kind.Name,
            t.Status.Name,
            t.SuspensionReason,
            t.AccessProvisioning.Name,
            new TenantContactDto(t.Contact.Email, t.Contact.Phone),
            new TenantAddressDto(
                t.Address.ZipCode,
                t.Address.Street,
                t.Address.Number,
                t.Address.Complement,
                t.Address.Neighborhood,
                t.Address.City,
                t.Address.State,
                t.Address.Country),
            t.Products.Select(p => new TenantProductDto(p.Code.Name, p.IsActive, p.ActivatedAt, p.DeactivatedAt)).ToList(),
            t.Memberships
                .Select(m => new TenantMembershipDto(m.Email, m.Role.Name, m.IsActive, m.Provisioning.Name, m.IdentityUserId?.Value))
                .ToList(),
            t.CreatedAt,
            t.UpdatedAt);
}
