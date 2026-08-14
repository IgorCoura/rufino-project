namespace TenantManagement.UnitTests.Tenants;

using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.SharedKernel;
using TenantManagement.Domain.Tenants;
using TenantManagement.UnitTests.Tenants.Mothers;

public class TenantMembershipTests
{
    private const string SecondEmail = "socio@rufino.com.br";

    // Conceder acesso a alguém novo cria o vínculo pendente e emite MembershipGranted com o papel.
    [Fact]
    public void GrantMembership_ToNewEmail_ShouldAddPendingMembershipAndEmitEvent()
    {
        var tenant = TenantMother.Provisioned();

        tenant.GrantMembership(SecondEmail, MembershipRole.Member, TenantMother.DefaultOccurredAt);

        var membership = Assert.Single(tenant.Memberships, m => m.Email == SecondEmail);
        Assert.Equal(MembershipRole.Member, membership.Role);
        Assert.Equal(ProvisioningStatus.Pending, membership.Provisioning);

        var granted = Assert.IsType<MembershipGrantedDomainEvent>(Assert.Single(tenant.PullDomainEvents()));
        Assert.Equal(SecondEmail, granted.Email);
        Assert.Equal(nameof(MembershipRole.Member), granted.Role);
    }

    // O e-mail é normalizado: espaços e maiúsculas não criam um segundo vínculo para a mesma pessoa.
    [Fact]
    public void GrantMembership_WithDifferentCasing_ShouldReuseTheSameMembership()
    {
        var tenant = TenantMother.Provisioned();

        tenant.GrantMembership("  SOCIO@Rufino.com.BR ", MembershipRole.Member, TenantMother.DefaultOccurredAt);
        tenant.GrantMembership(SecondEmail, MembershipRole.Member, TenantMother.DefaultOccurredAt);

        Assert.Equal(2, tenant.Memberships.Count);
        Assert.Contains(tenant.Memberships, m => m.Email == SecondEmail);
    }

    // Reconceder o mesmo papel a quem já tem acesso não faz nada — o convite repetido é tolerado.
    [Fact]
    public void GrantMembership_WithSameRoleTwice_ShouldBeIdempotent()
    {
        var tenant = TenantMother.Provisioned();
        tenant.GrantMembership(SecondEmail, MembershipRole.Member, TenantMother.DefaultOccurredAt);
        tenant.ClearDomainEvents();

        tenant.GrantMembership(SecondEmail, MembershipRole.Member, TenantMother.DefaultOccurredAt);

        Assert.Equal(2, tenant.Memberships.Count);
        Assert.Empty(tenant.PullDomainEvents());
    }

    // Conceder outro papel a quem já tem acesso só troca o papel, sem duplicar o vínculo.
    [Fact]
    public void GrantMembership_WithDifferentRole_ShouldChangeRoleInPlace()
    {
        var tenant = TenantMother.Provisioned();
        tenant.GrantMembership(SecondEmail, MembershipRole.Member, TenantMother.DefaultOccurredAt);
        tenant.ClearDomainEvents();

        tenant.GrantMembership(SecondEmail, MembershipRole.Owner, TenantMother.DefaultOccurredAt);

        var membership = Assert.Single(tenant.Memberships, m => m.Email == SecondEmail);
        Assert.Equal(MembershipRole.Owner, membership.Role);
        Assert.Empty(tenant.PullDomainEvents());
    }

    // Reconceder acesso a quem foi revogado reaproveita o vínculo e volta a pendente de provisionamento.
    [Fact]
    public void GrantMembership_AfterRevoke_ShouldReactivateTheSameMembership()
    {
        var tenant = TenantMother.Provisioned();
        tenant.GrantMembership(SecondEmail, MembershipRole.Member, TenantMother.DefaultOccurredAt);
        tenant.ConfirmAccessProvisioned(SecondEmail, UserId.New(), TenantMother.DefaultOccurredAt);
        tenant.RevokeMembership(SecondEmail, TenantMother.DefaultOccurredAt);
        tenant.ClearDomainEvents();

        tenant.GrantMembership(SecondEmail, MembershipRole.Member, TenantMother.DefaultOccurredAt);

        var membership = Assert.Single(tenant.Memberships, m => m.Email == SecondEmail);
        Assert.True(membership.IsActive);
        Assert.Null(membership.RevokedAt);
        Assert.Equal(ProvisioningStatus.Pending, membership.Provisioning);
        Assert.IsType<MembershipGrantedDomainEvent>(Assert.Single(tenant.PullDomainEvents()));
    }

    // E-mail vazio ou inválido reprova a concessão em TNM.TNT18 e TNM.TNT19.
    [Theory]
    [InlineData("", "TNM.TNT18")]
    [InlineData("   ", "TNM.TNT18")]
    [InlineData("sem-arroba", "TNM.TNT19")]
    [InlineData("dois@@arrobas.com", "TNM.TNT19")]
    [InlineData("sem@dominio", "TNM.TNT19")]
    public void GrantMembership_WithInvalidEmail_ShouldThrowMatchingError(string email, string expectedErrorId)
    {
        var tenant = TenantMother.Provisioned();

        var error = Assert.Throws<DomainException>(() =>
            tenant.GrantMembership(email, MembershipRole.Member, TenantMother.DefaultOccurredAt));

        Assert.Equal(expectedErrorId, error.Id);
    }

    // Conceder acesso sem papel é reprovado em TNM.TNT22.
    [Fact]
    public void GrantMembership_WithoutRole_ShouldThrow_TNM_TNT22()
    {
        var tenant = TenantMother.Provisioned();

        var error = Assert.Throws<DomainException>(() =>
            tenant.GrantMembership(SecondEmail, null!, TenantMother.DefaultOccurredAt));

        Assert.Equal("TNM.TNT22", error.Id);
    }

    // Revogar desativa o vínculo, guarda a data e emite MembershipRevoked.
    [Fact]
    public void RevokeMembership_WhenAnotherOwnerExists_ShouldDeactivateAndEmitEvent()
    {
        var tenant = TenantMother.Provisioned();
        tenant.GrantMembership(SecondEmail, MembershipRole.Owner, TenantMother.DefaultOccurredAt);
        tenant.ClearDomainEvents();
        var later = TenantMother.DefaultOccurredAt.AddDays(1);

        tenant.RevokeMembership(SecondEmail, later);

        var membership = Assert.Single(tenant.Memberships, m => m.Email == SecondEmail);
        Assert.False(membership.IsActive);
        Assert.Equal(later, membership.RevokedAt);

        var revoked = Assert.IsType<MembershipRevokedDomainEvent>(Assert.Single(tenant.PullDomainEvents()));
        Assert.Equal(SecondEmail, revoked.Email);
    }

    // Revogar o último responsável é reprovado em TNM.TNT20 — o tenant ficaria sem quem responda por ele.
    [Fact]
    public void RevokeMembership_WhenLastOwner_ShouldThrow_TNM_TNT20()
    {
        var tenant = TenantMother.Provisioned();

        var error = Assert.Throws<DomainException>(() =>
            tenant.RevokeMembership(TenantMother.DefaultOwnerEmail, TenantMother.DefaultOccurredAt));

        Assert.Equal("TNM.TNT20", error.Id);
    }

    // Rebaixar o último responsável a membro comum também é reprovado em TNM.TNT20.
    [Fact]
    public void GrantMembership_DemotingLastOwner_ShouldThrow_TNM_TNT20()
    {
        var tenant = TenantMother.Provisioned();

        var error = Assert.Throws<DomainException>(() =>
            tenant.GrantMembership(TenantMother.DefaultOwnerEmail, MembershipRole.Member, TenantMother.DefaultOccurredAt));

        Assert.Equal("TNM.TNT20", error.Id);
    }

    // Revogar acesso de quem não tem é reprovado em TNM.TNT21.
    [Fact]
    public void RevokeMembership_ForUnknownEmail_ShouldThrow_TNM_TNT21()
    {
        var tenant = TenantMother.Provisioned();

        var error = Assert.Throws<DomainException>(() =>
            tenant.RevokeMembership("ninguem@rufino.com.br", TenantMother.DefaultOccurredAt));

        Assert.Equal("TNM.TNT21", error.Id);
    }

    // Confirmar o provisionamento grava o identificador do provedor e tira o vínculo de pendente.
    [Fact]
    public void ConfirmAccessProvisioned_WithUserId_ShouldStoreItAndMarkDone()
    {
        var tenant = TenantMother.Register();
        var userId = UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000bb"));

        tenant.ConfirmAccessProvisioned(TenantMother.DefaultOwnerEmail, userId, TenantMother.DefaultOccurredAt);

        var owner = Assert.Single(tenant.Memberships);
        Assert.Equal(userId, owner.IdentityUserId);
        Assert.Equal(ProvisioningStatus.Done, owner.Provisioning);
        Assert.Equal(ProvisioningStatus.Done, tenant.AccessProvisioning);
    }

    // Um vínculo falho contamina o estado do tenant — é o que faz a falha aparecer na listagem.
    [Fact]
    public void AccessProvisioning_WhenAnyMembershipFailed_ShouldReportFailed()
    {
        var tenant = TenantMother.Provisioned();
        tenant.GrantMembership(SecondEmail, MembershipRole.Member, TenantMother.DefaultOccurredAt);

        tenant.MarkAccessProvisioningFailed(SecondEmail, TenantMother.DefaultOccurredAt);

        Assert.Equal(ProvisioningStatus.Failed, tenant.AccessProvisioning);
    }

    // Reprovisionar recoloca na fila só o que não chegou ao provedor, reemitindo o evento correspondente.
    [Fact]
    public void RequeueFailedAccessProvisioning_ShouldReemitOnlyPendingMemberships()
    {
        var tenant = TenantMother.Provisioned();
        tenant.GrantMembership(SecondEmail, MembershipRole.Member, TenantMother.DefaultOccurredAt);
        tenant.MarkAccessProvisioningFailed(SecondEmail, TenantMother.DefaultOccurredAt);
        tenant.ClearDomainEvents();

        var requeued = tenant.RequeueFailedAccessProvisioning(TenantMother.DefaultOccurredAt);

        Assert.Single(requeued);
        Assert.Equal(ProvisioningStatus.Pending, tenant.AccessProvisioning);
        var granted = Assert.IsType<MembershipGrantedDomainEvent>(Assert.Single(tenant.PullDomainEvents()));
        Assert.Equal(SecondEmail, granted.Email);
    }

    // Reprovisionar quando está tudo concluído não faz nada e não emite evento.
    [Fact]
    public void RequeueFailedAccessProvisioning_WhenNothingPending_ShouldDoNothing()
    {
        var tenant = TenantMother.Provisioned();

        var requeued = tenant.RequeueFailedAccessProvisioning(TenantMother.DefaultOccurredAt);

        Assert.Empty(requeued);
        Assert.Empty(tenant.PullDomainEvents());
    }

    // Um vínculo revogado que não chegou ao provedor volta à fila como revogação, não como concessão.
    [Fact]
    public void RequeueFailedAccessProvisioning_ForRevokedMembership_ShouldReemitRevoked()
    {
        var tenant = TenantMother.Provisioned();
        tenant.GrantMembership(SecondEmail, MembershipRole.Owner, TenantMother.DefaultOccurredAt);
        tenant.ConfirmAccessProvisioned(SecondEmail, UserId.New(), TenantMother.DefaultOccurredAt);
        tenant.RevokeMembership(SecondEmail, TenantMother.DefaultOccurredAt);
        tenant.ClearDomainEvents();

        tenant.RequeueFailedAccessProvisioning(TenantMother.DefaultOccurredAt);

        Assert.IsType<MembershipRevokedDomainEvent>(Assert.Single(tenant.PullDomainEvents()));
    }

    // Tenant suspenso não distribui acesso novo (TNM.TNT12).
    [Fact]
    public void GrantMembership_WhenSuspended_ShouldThrow_TNM_TNT12()
    {
        var tenant = TenantMother.Provisioned();
        tenant.Suspend("Inadimplência", TenantMother.DefaultOccurredAt);

        var error = Assert.Throws<DomainException>(() =>
            tenant.GrantMembership(SecondEmail, MembershipRole.Member, TenantMother.DefaultOccurredAt));

        Assert.Equal("TNM.TNT12", error.Id);
    }

    // Revogar acesso continua permitido em tenant suspenso — cortar acesso nunca pode ficar bloqueado.
    [Fact]
    public void RevokeMembership_WhenSuspended_ShouldStillWork()
    {
        var tenant = TenantMother.Provisioned();
        tenant.GrantMembership(SecondEmail, MembershipRole.Owner, TenantMother.DefaultOccurredAt);
        tenant.Suspend("Inadimplência", TenantMother.DefaultOccurredAt);

        tenant.RevokeMembership(SecondEmail, TenantMother.DefaultOccurredAt);

        Assert.False(Assert.Single(tenant.Memberships, m => m.Email == SecondEmail).IsActive);
    }

    // HasActiveMember responde pelo e-mail normalizado e ignora quem foi revogado.
    [Fact]
    public void HasActiveMember_ShouldConsiderOnlyActiveMemberships()
    {
        var tenant = TenantMother.Provisioned();
        tenant.GrantMembership(SecondEmail, MembershipRole.Owner, TenantMother.DefaultOccurredAt);
        tenant.RevokeMembership(SecondEmail, TenantMother.DefaultOccurredAt);

        Assert.True(tenant.HasActiveMember(" TITULAR@Rufino.com.br "));
        Assert.False(tenant.HasActiveMember(SecondEmail));
    }
}
