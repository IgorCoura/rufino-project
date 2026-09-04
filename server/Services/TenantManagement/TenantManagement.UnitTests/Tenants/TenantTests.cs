namespace TenantManagement.UnitTests.Tenants;

using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.SharedKernel;
using TenantManagement.Domain.Tenants;
using TenantManagement.UnitTests.Tenants.Mothers;

public class TenantTests
{
    // Cadastrar pessoa jurídica guarda razão social, CNPJ, contato e endereço, e nasce ativa.
    [Fact]
    public void Register_WithCompanyData_ShouldStoreIdentityAndStartActive()
    {
        var tenant = TenantMother.Register(tradeName: TenantMother.DefaultTradeName);

        Assert.Equal(TenantKind.Company, tenant.Kind);
        Assert.Equal(TenantMother.DefaultCompanyName, tenant.LegalName);
        Assert.Equal(TenantMother.DefaultTradeName, tenant.TradeName);
        Assert.Equal(TenantMother.DefaultCnpj, tenant.PrimaryTaxId.Value);
        Assert.Equal("01310100", tenant.Address.ZipCode);
        Assert.Equal("contato@rufino.com.br", tenant.Contact.Email);
        Assert.Equal(TenantStatus.Active, tenant.Status);
    }

    // Pessoa física entra pelo mesmo cadastro, com CPF — é a razão de este BC existir.
    [Fact]
    public void Register_WithIndividualData_ShouldAcceptCpf()
    {
        var tenant = TenantMother.Individual();

        Assert.Equal(TenantKind.Individual, tenant.Kind);
        Assert.Equal(TaxIdKind.CPF, tenant.PrimaryTaxId.Kind);
        Assert.Equal(TenantMother.DefaultCpf, tenant.PrimaryTaxId.Value);
    }

    // O Id do tenant pode ser informado: é o que permite migrar um cadastro existente sem reemitir acesso.
    [Fact]
    public void Register_WithInformedId_ShouldPreserveIt()
    {
        var id = TenantId.From(new Guid("0195a1f0-0000-7000-8000-00000000beef"));

        var tenant = TenantMother.Register(id: id);

        Assert.Equal(id, tenant.Id);
    }

    // Pessoa jurídica com CPF é reprovada em TNM.TNT05 — a única diferença entre PF e PJ no cadastro.
    [Fact]
    public void Register_CompanyWithCpf_ShouldThrow_TNM_TNT05()
    {
        var error = Assert.Throws<DomainException>(() =>
            TenantMother.Register(TenantKind.Company, primaryTaxId: TenantMother.Cpf()));

        Assert.Equal("TNM.TNT05", error.Id);
    }

    // Pessoa física com CNPJ também é reprovada em TNM.TNT05.
    [Fact]
    public void Register_IndividualWithCnpj_ShouldThrow_TNM_TNT05()
    {
        var error = Assert.Throws<DomainException>(() =>
            TenantMother.Register(TenantKind.Individual, primaryTaxId: TenantMother.Cnpj()));

        Assert.Equal("TNM.TNT05", error.Id);
    }

    // Nome fantasia em pessoa física é reprovado em TNM.TNT06.
    [Fact]
    public void Register_IndividualWithTradeName_ShouldThrow_TNM_TNT06()
    {
        var error = Assert.Throws<DomainException>(() => TenantMother.Register(
            TenantKind.Individual,
            TenantMother.DefaultIndividualName,
            tradeName: "APELIDO",
            primaryTaxId: TenantMother.Cpf()));

        Assert.Equal("TNM.TNT06", error.Id);
    }

    // Nome em branco reprova o cadastro em TNM.TNT02.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithoutLegalName_ShouldThrow_TNM_TNT02(string legalName)
    {
        var error = Assert.Throws<DomainException>(() => TenantMother.Register(legalName: legalName));

        Assert.Equal("TNM.TNT02", error.Id);
    }

    // Nome maior que o limite reprova em TNM.TNT03.
    [Fact]
    public void Register_WithOversizedLegalName_ShouldThrow_TNM_TNT03()
    {
        var error = Assert.Throws<DomainException>(() =>
            TenantMother.Register(legalName: new string('A', Tenant.LEGAL_NAME_MAX_LENGTH + 1)));

        Assert.Equal("TNM.TNT03", error.Id);
    }

    // Cadastro sem tipo, sem documento, sem contato e sem endereço reprova cada um no seu erro.
    [Theory]
    [InlineData(null, "TNM.TNT01")]
    [InlineData("taxId", "TNM.TNT04")]
    [InlineData("contact", "TNM.TNT08")]
    [InlineData("address", "TNM.TNT09")]
    public void Register_WithMissingRequiredPart_ShouldThrowMatchingError(string? missingPart, string expectedErrorId)
    {
        var error = Assert.Throws<DomainException>(() => TenantMother.RegisterVerbatim(
            kind: missingPart is null ? null! : TenantKind.Company,
            legalName: TenantMother.DefaultCompanyName,
            tradeName: null,
            primaryTaxId: missingPart == "taxId" ? null! : TenantMother.Cnpj(),
            contact: missingPart == "contact" ? null! : TenantMother.Contact(),
            address: missingPart == "address" ? null! : TenantMother.Address(),
            ownerEmail: TenantMother.DefaultOwnerEmail));

        Assert.Equal(expectedErrorId, error.Id);
    }

    // Cadastrar emite TenantRegistered com nome e tipo, seguido do MembershipGranted do titular.
    [Fact]
    public void Register_ShouldEmitRegisteredAndOwnerMembershipEvents()
    {
        var tenant = TenantMother.Register();

        var events = tenant.PullDomainEvents();

        var registered = Assert.IsType<TenantRegisteredDomainEvent>(events[0]);
        Assert.Equal(TenantMother.DefaultId, registered.TenantId);
        Assert.Equal(TenantMother.DefaultCompanyName, registered.LegalName);
        Assert.Equal(nameof(TenantKind.Company), registered.Kind);
        Assert.Equal(TenantMother.DefaultOccurredAt, registered.OccurredAt);

        var granted = Assert.IsType<MembershipGrantedDomainEvent>(events[1]);
        Assert.Equal(TenantMother.DefaultOwnerEmail, granted.Email);
        Assert.Equal(nameof(MembershipRole.Owner), granted.Role);
        Assert.Equal(TenantMother.DefaultCompanyName, granted.TenantLegalName);
        Assert.Equal(2, events.Count);
    }

    // O titular entra como responsável já no cadastro, e o acesso nasce pendente de provisionamento.
    [Fact]
    public void Register_ShouldGrantOwnerMembershipPendingProvisioning()
    {
        var tenant = TenantMother.Register();

        var owner = Assert.Single(tenant.Memberships);
        Assert.Equal(TenantMother.DefaultOwnerEmail, owner.Email);
        Assert.Equal(MembershipRole.Owner, owner.Role);
        Assert.True(owner.IsActive);
        Assert.Null(owner.IdentityUserId);
        Assert.Equal(ProvisioningStatus.Pending, owner.Provisioning);
        Assert.Equal(ProvisioningStatus.Pending, tenant.AccessProvisioning);
    }

    // Renomear troca razão social e nome fantasia e move UpdatedAt.
    [Fact]
    public void Rename_WithNewNames_ShouldReplaceThem()
    {
        var tenant = TenantMother.Provisioned();
        var later = TenantMother.DefaultOccurredAt.AddDays(1);

        tenant.Rename("RUFINO ENGENHARIA LTDA", "RUFINO ENG", later);

        Assert.Equal("RUFINO ENGENHARIA LTDA", tenant.LegalName);
        Assert.Equal("RUFINO ENG", tenant.TradeName);
        Assert.Equal(later, tenant.UpdatedAt);
    }

    // Trocar o endereço substitui o VO inteiro — endereço não se edita campo a campo.
    [Fact]
    public void ChangeAddress_WithNewAddress_ShouldReplaceIt()
    {
        var tenant = TenantMother.Provisioned();
        var later = TenantMother.DefaultOccurredAt.AddDays(1);

        tenant.ChangeAddress(TenantMother.Address(zipCode: "30130010", city: "Belo Horizonte", state: "mg"), later);

        Assert.Equal("30130010", tenant.Address.ZipCode);
        Assert.Equal("BELO HORIZONTE", tenant.Address.City);
        Assert.Equal("MG", tenant.Address.State);
        Assert.Equal(later, tenant.UpdatedAt);
    }

    // Trocar o contato substitui e-mail e telefone.
    [Fact]
    public void ChangeContact_WithNewContact_ShouldReplaceIt()
    {
        var tenant = TenantMother.Provisioned();

        tenant.ChangeContact(TenantMother.Contact("financeiro@rufino.com.br", "1133224455"), TenantMother.DefaultOccurredAt);

        Assert.Equal("financeiro@rufino.com.br", tenant.Contact.Email);
        Assert.Equal("1133224455", tenant.Contact.Phone);
    }

    // Suspender exige motivo, corta o tenant e emite TenantSuspended com o motivo registrado.
    [Fact]
    public void Suspend_WithReason_ShouldChangeStatusAndEmitEvent()
    {
        var tenant = TenantMother.Provisioned();
        var later = TenantMother.DefaultOccurredAt.AddDays(2);

        tenant.Suspend("Inadimplência", later);

        Assert.Equal(TenantStatus.Suspended, tenant.Status);
        Assert.Equal("Inadimplência", tenant.SuspensionReason);

        var suspended = Assert.IsType<TenantSuspendedDomainEvent>(Assert.Single(tenant.PullDomainEvents()));
        Assert.Equal(TenantMother.DefaultId, suspended.TenantId);
        Assert.Equal("Inadimplência", suspended.Reason);
        Assert.Equal(later, suspended.OccurredAt);
    }

    // Suspender sem motivo é reprovado em TNM.TNT15 — suspensão sem registro é suspensão sem responsável.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Suspend_WithoutReason_ShouldThrow_TNM_TNT15(string reason)
    {
        var tenant = TenantMother.Provisioned();

        var error = Assert.Throws<DomainException>(() => tenant.Suspend(reason, TenantMother.DefaultOccurredAt));

        Assert.Equal("TNM.TNT15", error.Id);
    }

    // Suspender duas vezes é reprovado em TNM.TNT13.
    [Fact]
    public void Suspend_WhenAlreadySuspended_ShouldThrow_TNM_TNT13()
    {
        var tenant = TenantMother.Provisioned();
        tenant.Suspend("Inadimplência", TenantMother.DefaultOccurredAt);

        var error = Assert.Throws<DomainException>(() => tenant.Suspend("De novo", TenantMother.DefaultOccurredAt));

        Assert.Equal("TNM.TNT13", error.Id);
    }

    // Tenant suspenso não aceita alteração de cadastro (TNM.TNT12) — reative antes.
    [Fact]
    public void Rename_WhenSuspended_ShouldThrow_TNM_TNT12()
    {
        var tenant = TenantMother.Provisioned();
        tenant.Suspend("Inadimplência", TenantMother.DefaultOccurredAt);

        var error = Assert.Throws<DomainException>(() =>
            tenant.Rename("OUTRO NOME", null, TenantMother.DefaultOccurredAt));

        Assert.Equal("TNM.TNT12", error.Id);
    }

    // Reativar limpa o motivo, devolve o tenant ao ar e emite TenantReactivated.
    [Fact]
    public void Reactivate_WhenSuspended_ShouldReturnToActive()
    {
        var tenant = TenantMother.Provisioned();
        tenant.Suspend("Inadimplência", TenantMother.DefaultOccurredAt);
        tenant.ClearDomainEvents();

        tenant.Reactivate(TenantMother.DefaultOccurredAt.AddDays(3));

        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.Equal(string.Empty, tenant.SuspensionReason);
        Assert.IsType<TenantReactivatedDomainEvent>(Assert.Single(tenant.PullDomainEvents()));
    }

    // Reativar um tenant que está ativo é reprovado em TNM.TNT14.
    [Fact]
    public void Reactivate_WhenActive_ShouldThrow_TNM_TNT14()
    {
        var tenant = TenantMother.Provisioned();

        var error = Assert.Throws<DomainException>(() => tenant.Reactivate(TenantMother.DefaultOccurredAt));

        Assert.Equal("TNM.TNT14", error.Id);
    }
}
