namespace AccountsPayable.UnitTests.Suppliers;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;
using AccountsPayable.Domain.Suppliers.Entities;
using AccountsPayable.Domain.Suppliers.Enumerations;
using AccountsPayable.Domain.Suppliers.Events;
using AccountsPayable.Domain.Suppliers.ValueObjects;
using AccountsPayable.UnitTests.Suppliers.Mothers;

public class SupplierTests
{
    private static readonly DateTime FIXED_NOW = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime LATER = FIXED_NOW.AddMinutes(5);

    public class WhenCreating
    {
        // Create monta o Supplier no estado Active, preserva todos os VOs e timestamps.
        [Fact]
        public void Create_WithValidData_ShouldReturnSupplierInActiveStatus()
        {
            var supplier = SupplierMother.Active();

            Assert.Equal(SupplierStatus.Active, supplier.Status);
            Assert.Equal("Acme Brasil LTDA", supplier.LegalName.Value);
            Assert.Equal("Acme", supplier.TradeName?.Value);
            Assert.Equal("11444777000161", supplier.TaxId.Value);
            Assert.Equal("contato@acme.com.br", supplier.Contact.Email.Value);
            Assert.Equal(SupplierMother.DEFAULT_OCCURRED_AT, supplier.CreatedAt);
            Assert.Equal(SupplierMother.DEFAULT_OCCURRED_AT, supplier.UpdatedAt);
        }

        // Create emite SupplierCreated com o payload completo (incluindo tipo do TaxId em string).
        [Fact]
        public void Create_ShouldEmitSupplierCreatedEvent()
        {
            var supplier = SupplierMother.Active();

            var events = supplier.PullDomainEvents();
            var created = Assert.Single(events);
            var e = Assert.IsType<SupplierCreated>(created);
            Assert.Equal(supplier.Id, e.SupplierId);
            Assert.Equal(SupplierMother.DEFAULT_TENANT, e.TenantId);
            Assert.Equal("Acme Brasil LTDA", e.LegalName);
            Assert.Equal("Acme", e.TradeName);
            Assert.Equal("11444777000161", e.TaxIdValue);
            Assert.Equal("CNPJ", e.TaxIdType);
            Assert.Equal(SupplierMother.DEFAULT_OCCURRED_AT, e.OccurredAt);
        }

        // Create sem TradeName aceita null e emite evento com TradeName null no payload.
        [Fact]
        public void Create_WithoutTradeName_ShouldStoreNullAndEmitNullInEvent()
        {
            var supplier = SupplierMother.Active(tradeName: null);

            Assert.Null(supplier.TradeName);
            var e = Assert.IsType<SupplierCreated>(supplier.PullDomainEvents()[0]);
            Assert.Null(e.TradeName);
        }
    }

    public class WhenRenaming
    {
        // Rename muda LegalName, atualiza UpdatedAt e emite SupplierRenamed com nome antigo e novo.
        [Fact]
        public void Rename_WithDifferentName_ShouldUpdateLegalNameAndEmitEvent()
        {
            var supplier = SupplierMother.Active();
            supplier.PullDomainEvents();

            supplier.Rename(new LegalName("Acme Reformulada LTDA"), LATER);

            Assert.Equal("Acme Reformulada LTDA", supplier.LegalName.Value);
            Assert.Equal(LATER, supplier.UpdatedAt);
            var renamed = Assert.IsType<SupplierRenamed>(supplier.PullDomainEvents()[0]);
            Assert.Equal("Acme Brasil LTDA", renamed.OldLegalName);
            Assert.Equal("Acme Reformulada LTDA", renamed.NewLegalName);
        }

        // Rename com o mesmo nome atual é idempotente: não emite evento nem altera UpdatedAt.
        [Fact]
        public void Rename_WithSameName_ShouldBeNoOp()
        {
            var supplier = SupplierMother.Active();
            supplier.PullDomainEvents();
            var prevUpdatedAt = supplier.UpdatedAt;

            supplier.Rename(new LegalName("Acme Brasil LTDA"), LATER);

            Assert.Empty(supplier.PullDomainEvents());
            Assert.Equal(prevUpdatedAt, supplier.UpdatedAt);
        }
    }

    public class WhenUpdatingContact
    {
        // UpdateContact substitui o contato, atualiza UpdatedAt e emite SupplierContactUpdated com novo email/phone.
        [Fact]
        public void UpdateContact_WithDifferentContact_ShouldReplaceAndEmitEvent()
        {
            var supplier = SupplierMother.Active();
            supplier.PullDomainEvents();
            var newContact = new ContactInfo(
                new EmailAddress("novo@acme.com.br"),
                new PhoneNumber("11987654321"));

            supplier.UpdateContact(newContact, LATER);

            Assert.Equal("novo@acme.com.br", supplier.Contact.Email.Value);
            Assert.Equal(LATER, supplier.UpdatedAt);
            var updated = Assert.IsType<SupplierContactUpdated>(supplier.PullDomainEvents()[0]);
            Assert.Equal("novo@acme.com.br", updated.Email);
            Assert.Equal("11987654321", updated.Phone);
        }

        // UpdateContact com contato igual ao atual é idempotente: sem evento, sem UpdatedAt novo.
        [Fact]
        public void UpdateContact_WithSameContact_ShouldBeNoOp()
        {
            var supplier = SupplierMother.Active();
            supplier.PullDomainEvents();

            supplier.UpdateContact(new ContactInfo(new EmailAddress("contato@acme.com.br")), LATER);

            Assert.Empty(supplier.PullDomainEvents());
        }
    }

    public class WhenAddingBankAccount
    {
        // AddBankAccount cria SupplierBankAccount, devolve a entidade e emite SupplierBankAccountAdded.
        [Fact]
        public void AddBankAccount_WithValidData_ShouldAddAndEmitEvent()
        {
            var supplier = SupplierMother.Active();
            supplier.PullDomainEvents();
            var bankAccountId = SupplierBankAccountId.New();

            var account = supplier.AddBankAccount(
                bankAccountId, "001", "0001", "123456-7", BankAccountType.Checking, pixKey: null, LATER);

            Assert.Same(account, supplier.BankAccounts.Single());
            Assert.Equal("001", account.BankCode);
            Assert.Equal("0001", account.Branch);
            Assert.Equal("123456-7", account.AccountNumber);
            var added = Assert.IsType<SupplierBankAccountAdded>(supplier.PullDomainEvents()[0]);
            Assert.Equal(bankAccountId, added.BankAccountId);
            Assert.Equal("CHECKING", added.AccountType);
        }

        // Adicionar conta com mesma (banco, agência, número) que outra já existente lança AP.SUP05.
        [Fact]
        public void AddBankAccount_WithDuplicateCombo_ShouldThrowDomainException()
        {
            var supplier = SupplierMother.ActiveWithBankAccount();

            var ex = Assert.Throws<DomainException>(() => supplier.AddBankAccount(
                SupplierBankAccountId.New(), "001", "0001", "123456-7", BankAccountType.Checking, pixKey: null, LATER));

            Assert.Equal("AP.SUP05", ex.Id);
        }
    }

    public class WhenRemovingBankAccount
    {
        // Remover uma conta inexistente (id desconhecido) lança AP.SUP04.
        [Fact]
        public void RemoveBankAccount_WithUnknownId_ShouldThrowDomainException()
        {
            var supplier = SupplierMother.ActiveWithBankAccount();

            var ex = Assert.Throws<DomainException>(
                () => supplier.RemoveBankAccount(SupplierBankAccountId.New(), LATER));

            Assert.Equal("AP.SUP04", ex.Id);
        }

        // Remover a última conta de um Supplier Active lança AP.SUP03 (regra "fornecedor ativo precisa ter conta").
        [Fact]
        public void RemoveBankAccount_WhenLastAccountAndActive_ShouldThrowDomainException()
        {
            var supplier = SupplierMother.ActiveWithBankAccount();
            var account = supplier.BankAccounts.Single();

            var ex = Assert.Throws<DomainException>(() => supplier.RemoveBankAccount(account.Id, LATER));

            Assert.Equal("AP.SUP03", ex.Id);
        }

        // Remover uma conta quando ainda há outras restantes funciona e emite SupplierBankAccountRemoved.
        [Fact]
        public void RemoveBankAccount_WhenMoreThanOneAccount_ShouldRemoveAndEmitEvent()
        {
            var supplier = SupplierMother.ActiveWithBankAccounts(count: 2);
            supplier.PullDomainEvents();
            var firstId = supplier.BankAccounts.First().Id;

            supplier.RemoveBankAccount(firstId, LATER);

            Assert.Single(supplier.BankAccounts);
            var removed = Assert.IsType<SupplierBankAccountRemoved>(supplier.PullDomainEvents()[0]);
            Assert.Equal(firstId, removed.BankAccountId);
        }

        // Remover a última conta quando o Supplier está Inactive é permitido (regra só vale para Active).
        [Fact]
        public void RemoveBankAccount_WhenLastAccountAndInactive_ShouldRemoveSuccessfully()
        {
            var supplier = SupplierMother.Active();
            var accountId = SupplierBankAccountId.New();
            supplier.AddBankAccount(accountId, "001", "0001", "123456-7", BankAccountType.Checking, null, LATER);
            supplier.Deactivate(LATER.AddMinutes(1));
            supplier.PullDomainEvents();

            supplier.RemoveBankAccount(accountId, LATER.AddMinutes(2));

            Assert.Empty(supplier.BankAccounts);
        }
    }

    public class WhenBlocking
    {
        // Block muda status para Blocked, emite SupplierBlocked com a razão informada.
        [Fact]
        public void Block_FromActive_ShouldChangeStatusAndEmitEvent()
        {
            var supplier = SupplierMother.Active();
            supplier.PullDomainEvents();

            supplier.Block("Inadimplência", LATER);

            Assert.Equal(SupplierStatus.Blocked, supplier.Status);
            var blocked = Assert.IsType<SupplierBlocked>(supplier.PullDomainEvents()[0]);
            Assert.Equal("Inadimplência", blocked.Reason);
        }

        // Block exige motivo não vazio — string vazia/branca lança AP.SUP02.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Block_WithEmptyReason_ShouldThrowDomainException(string reason)
        {
            var supplier = SupplierMother.Active();

            var ex = Assert.Throws<DomainException>(() => supplier.Block(reason, LATER));

            Assert.Equal("AP.SUP02", ex.Id);
        }

        // Block sobre um Supplier já Blocked lança AP.SUP01 (transição inválida).
        [Fact]
        public void Block_WhenAlreadyBlocked_ShouldThrowDomainException()
        {
            var supplier = SupplierMother.Blocked();

            var ex = Assert.Throws<DomainException>(() => supplier.Block("Outra razão", LATER));

            Assert.Equal("AP.SUP01", ex.Id);
        }

        // Block sobre Supplier Inactive não é permitido (transição Inactive->Blocked é inválida).
        [Fact]
        public void Block_WhenInactive_ShouldThrowDomainException()
        {
            var supplier = SupplierMother.Inactive();

            var ex = Assert.Throws<DomainException>(() => supplier.Block("Razão", LATER));

            Assert.Equal("AP.SUP01", ex.Id);
        }
    }

    public class WhenUnblocking
    {
        // Unblock de Supplier Blocked retorna o status para Active e emite SupplierUnblocked.
        [Fact]
        public void Unblock_FromBlocked_ShouldChangeStatusAndEmitEvent()
        {
            var supplier = SupplierMother.Blocked();
            supplier.PullDomainEvents();

            supplier.Unblock(LATER);

            Assert.Equal(SupplierStatus.Active, supplier.Status);
            Assert.IsType<SupplierUnblocked>(supplier.PullDomainEvents()[0]);
        }

        // Unblock em status que não é Blocked lança AP.SUP01.
        [Fact]
        public void Unblock_WhenActive_ShouldThrowDomainException()
        {
            var supplier = SupplierMother.Active();

            var ex = Assert.Throws<DomainException>(() => supplier.Unblock(LATER));

            Assert.Equal("AP.SUP01", ex.Id);
        }
    }

    public class WhenDeactivating
    {
        // Deactivate de Active vai para Inactive e emite SupplierDeactivated.
        [Fact]
        public void Deactivate_FromActive_ShouldChangeStatusAndEmitEvent()
        {
            var supplier = SupplierMother.Active();
            supplier.PullDomainEvents();

            supplier.Deactivate(LATER);

            Assert.Equal(SupplierStatus.Inactive, supplier.Status);
            Assert.IsType<SupplierDeactivated>(supplier.PullDomainEvents()[0]);
        }

        // Deactivate em status não-Active lança AP.SUP01 (transição inválida de Blocked/Inactive).
        [Fact]
        public void Deactivate_WhenBlocked_ShouldThrowDomainException()
        {
            var supplier = SupplierMother.Blocked();

            var ex = Assert.Throws<DomainException>(() => supplier.Deactivate(LATER));

            Assert.Equal("AP.SUP01", ex.Id);
        }
    }

    public class WhenReactivating
    {
        // Reactivate de Inactive retorna para Active e emite SupplierReactivated.
        [Fact]
        public void Reactivate_FromInactive_ShouldChangeStatusAndEmitEvent()
        {
            var supplier = SupplierMother.Inactive();
            supplier.PullDomainEvents();

            supplier.Reactivate(LATER);

            Assert.Equal(SupplierStatus.Active, supplier.Status);
            Assert.IsType<SupplierReactivated>(supplier.PullDomainEvents()[0]);
        }

        // Reactivate de status diferente de Inactive lança AP.SUP01.
        [Fact]
        public void Reactivate_WhenActive_ShouldThrowDomainException()
        {
            var supplier = SupplierMother.Active();

            var ex = Assert.Throws<DomainException>(() => supplier.Reactivate(LATER));

            Assert.Equal("AP.SUP01", ex.Id);
        }
    }
}
